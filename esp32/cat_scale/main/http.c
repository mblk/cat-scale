#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "http.h"
#include "hx711.h"
#include "time.h"

#include "sdkconfig.h"

#include <stdio.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/event_groups.h>

#include <esp_system.h>
#include <esp_event.h>
#include <esp_log.h>
#include <esp_tls.h>
#include <esp_http_client.h>

static const char *TAG = "app-http";

// #define MAX_HTTP_RECV_BUFFER 512
// #define MAX_HTTP_OUTPUT_BUFFER 2048

static esp_err_t http_client_event_handler(esp_http_client_event_t *evt)
{
    switch(evt->event_id) 
    {
        case HTTP_EVENT_ERROR:
            ESP_LOGE(TAG, "HTTP_EVENT_ERROR");
            break;

        case HTTP_EVENT_ON_CONNECTED:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_CONNECTED");
            break;

        case HTTP_EVENT_HEADER_SENT:
            ESP_LOGD(TAG, "HTTP_EVENT_HEADER_SENT");
            break;

        case HTTP_EVENT_ON_HEADER:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_HEADER, key=%s, value=%s", evt->header_key, evt->header_value);
            break;

        case HTTP_EVENT_ON_DATA:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_DATA, len=%d", evt->data_len);
            if (evt->data && evt->data_len > 0)
            {
                //ESP_LOG_BUFFER_HEX(TAG, evt->data, evt->data_len);

                char *temp = malloc(evt->data_len + 1);
                memcpy(temp, evt->data, evt->data_len);
                temp[evt->data_len] = 0;

                ESP_LOGD(TAG, "data: %s", temp);

                free(temp);
            }
            break;

        case HTTP_EVENT_ON_FINISH:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_FINISH");
            break;

        case HTTP_EVENT_DISCONNECTED:
            ESP_LOGD(TAG, "HTTP_EVENT_DISCONNECTED");
            break;

        case HTTP_EVENT_REDIRECT:
            ESP_LOGD(TAG, "HTTP_EVENT_REDIRECT");
            break;
    }
    return ESP_OK;
}

#if 0
esp_err_t http_post_sensor_data(const char *sensor_data)
{
    assert(sensor_data);

    esp_log_level_set(TAG, ESP_LOG_DEBUG);

    esp_http_client_config_t config = {
        .url = "http://Framework:8888/Sensor",
        .method = HTTP_METHOD_POST,
        .event_handler = http_client_event_handler,
    };

    esp_http_client_handle_t client = esp_http_client_init(&config);
    if (!client)
    {
        ESP_LOGE(TAG, "esp_http_client_init failed");
        return ESP_FAIL;
    }

    esp_http_client_set_header(client, "Content-Type", "application/json");
    esp_http_client_set_post_field(client, sensor_data, strlen(sensor_data));

    esp_err_t err = esp_http_client_perform(client);
    if (err == ESP_OK) {
        ESP_LOGI(TAG, "HTTP POST Status = %d", esp_http_client_get_status_code(client));

    } else {
        ESP_LOGE(TAG, "HTTP POST request failed: %s", esp_err_to_name(err));
    }

    esp_http_client_cleanup(client);

    return ESP_OK;
}
#endif

esp_err_t http_post_sensor_data_influx(const char *sensor_data)
{
    assert(sensor_data);

    //esp_log_level_set(TAG, ESP_LOG_DEBUG);

    const char *addr = CONFIG_CATSCALE_INFLUX_ENDPOINT;
    const char *org = CONFIG_CATSCALE_INFLUX_ORGANIZATION;
    const char *bucket = CONFIG_CATSCALE_INFLUX_BUCKET;
    const char *token = CONFIG_CATSCALE_INFLUX_TOKEN;

    char url[256] = {};
    snprintf(url, sizeof(url), "http://%s/api/v2/write?org=%s&bucket=%s&precision=ns", addr, org, bucket);

    char auth[256] = {};
    snprintf(auth, sizeof(auth), "Token %s", token);

    esp_http_client_config_t config = {
        .url = url,
        .method = HTTP_METHOD_POST,
        .event_handler = http_client_event_handler,
    };

    esp_http_client_handle_t client = esp_http_client_init(&config);
    if (!client)
    {
        ESP_LOGE(TAG, "esp_http_client_init failed");
        return ESP_FAIL;
    }

    esp_http_client_set_header(client, "Authorization", auth);
    esp_http_client_set_header(client, "Content-Type", "text/plain; charset=utf-8");
    esp_http_client_set_header(client, "Accept", "application/json");
    esp_http_client_set_post_field(client, sensor_data, strlen(sensor_data));

    esp_err_t err = esp_http_client_perform(client);
    if (err == ESP_OK) {
        ESP_LOGI(TAG, "HTTP POST Status = %d", esp_http_client_get_status_code(client));

    } else {
        ESP_LOGE(TAG, "HTTP POST request failed: %s", esp_err_to_name(err));
    }

    esp_http_client_cleanup(client);

    return ESP_OK;
}

esp_err_t http_post_json_data(const char *json)
{
    assert(json);

    esp_http_client_config_t config = {
        //.url = "http://Framework:5155/Measurement",
        .url = "http://Media:5155/Measurement",
        .method = HTTP_METHOD_POST,
        .event_handler = http_client_event_handler,
    };

    esp_http_client_handle_t client = esp_http_client_init(&config);
    if (!client)
    {
        ESP_LOGE(TAG, "esp_http_client_init failed");
        return ESP_FAIL;
    }

    esp_http_client_set_header(client, "Content-Type", "application/json");
    esp_http_client_set_post_field(client, json, strlen(json));

    esp_err_t err = esp_http_client_perform(client);
    if (err == ESP_OK) {
        ESP_LOGI(TAG, "HTTP POST Status = %d", esp_http_client_get_status_code(client));

    } else {
        ESP_LOGE(TAG, "HTTP POST request failed: %s", esp_err_to_name(err));
    }

    esp_http_client_cleanup(client);

    return ESP_OK;
}
