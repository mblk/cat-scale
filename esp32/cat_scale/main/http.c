#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "http.h"
#include "hx711.h"
#include "time.h"
#include "http_secrets.h"

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
#include <esp_crt_bundle.h>

static const char *TAG = "app-http";

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

                ESP_LOGI(TAG, "http data: %s", temp);

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

esp_err_t http_post_sensor_data_influx(const char *sensor_data)
{
    assert(sensor_data);

    //esp_log_level_set(TAG, ESP_LOG_DEBUG);

    const char *addr   = CONFIG_CATSCALE_INFLUX_ENDPOINT;
    const char *org    = CONFIG_CATSCALE_INFLUX_ORGANIZATION;
    const char *bucket = CONFIG_CATSCALE_INFLUX_BUCKET;
    const char *token  = CONFIG_CATSCALE_INFLUX_TOKEN;

    char url[256] = {};
    snprintf(url, sizeof(url), "http://%s/api/v2/write?org=%s&bucket=%s&precision=ns", addr, org, bucket);

    char auth[256] = {};
    snprintf(auth, sizeof(auth), "Token %s", token);

    const esp_http_client_config_t config = {
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

static esp_err_t http_post_json_data_with_endpoint(int endpoint, const char *path, const char *json)
{
    assert(path);
    assert(json);

    const char *addr = NULL;
    const char *token = NULL;
    get_http_secrets(endpoint, &addr, &token);
    if (!addr || !token) return ESP_FAIL;

    char url[256] = {};
    snprintf(url, sizeof(url), "%s/%s", addr, path);
    ESP_LOGI(TAG, "Posting to '%s' ...", url);

    const esp_http_client_config_t config = {
        .url = url,
        .method = HTTP_METHOD_POST,
        .event_handler = http_client_event_handler,
        .crt_bundle_attach = esp_crt_bundle_attach,
    };

    esp_http_client_handle_t client = esp_http_client_init(&config);
    if (!client) {
        ESP_LOGE(TAG, "esp_http_client_init failed");
        return ESP_FAIL;
    }

    esp_http_client_set_header(client, "Content-Type", "application/json");
    esp_http_client_set_header(client, "Authorization", "ApiKey");
    esp_http_client_set_header(client, "ApiKey", token);
    esp_http_client_set_post_field(client, json, strlen(json));

    esp_err_t ret = ESP_OK;
    esp_err_t err = esp_http_client_perform(client);
    if (err == ESP_OK) {
        int httpStatus = esp_http_client_get_status_code(client);
        ESP_LOGI(TAG, "HTTP POST Status = %d", httpStatus);
        if (httpStatus / 100 != 2)
            ret = ESP_FAIL;
    } else {
        ESP_LOGE(TAG, "HTTP POST request failed: %s", esp_err_to_name(err));
        ret = ESP_FAIL;
    }

    esp_http_client_cleanup(client);

    return ret;
}

esp_err_t http_post_json_data(const char *path, const char *json)
{
    esp_err_t ret = ESP_OK;

    for(int i=0; i<2; i++)
    {
        if (http_post_json_data_with_endpoint(i, path, json) != ESP_OK)
            ret = ESP_FAIL;
    }

    return ret;
}
