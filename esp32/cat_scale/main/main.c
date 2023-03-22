#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "sensors.h"
#include "wifi.h"
#include "http.h"
#include "time.h"
#include "rc.h"
#include "log_udp.h"

#include "sdkconfig.h"

#include <stdio.h>
#include <string.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/event_groups.h>

#include <esp_system.h>
#include <esp_event.h>
#include <esp_log.h>
#include <nvs_flash.h>

static const char *TAG = "main";

bool g_network_ready = false;

static esp_err_t flash_init();
static void dump_config();

void app_main()
{
    esp_log_level_set("*", ESP_LOG_INFO);

    ESP_LOGI(TAG, "Hello!");
    ESP_LOGI(TAG, "Minimum free heap size at start: %u KiB", esp_get_minimum_free_heap_size() / 1024);
    dump_config();
    
    ESP_ERROR_CHECK(log_udp_init());

    ESP_ERROR_CHECK(flash_init());

    ESP_ERROR_CHECK(wifi_init_sta());
    g_network_ready = true;

    ESP_ERROR_CHECK(time_init_and_sync());

    ESP_ERROR_CHECK(sensors_init());

    ESP_ERROR_CHECK(rc_init());

    while(true)
    {
        vTaskDelay(30 * 1000 / portTICK_PERIOD_MS);
        ESP_LOGI(TAG, "min free heap %u KiB", esp_get_minimum_free_heap_size() / 1024);
        wifi_check_health();
    }
}

static void dump_config()
{
    ESP_LOGI(TAG, "CONFIG:");
    ESP_LOGI(TAG, "  WIFI SSID:        %s", CONFIG_CATSCALE_WIFI_SSID);
    ESP_LOGI(TAG, "  WIFI PASSWORD:    %s", CONFIG_CATSCALE_WIFI_PASSWORD);
    ESP_LOGI(TAG, "  WIFI RETRY COUNT: %d", CONFIG_CATSCALE_WIFI_MAXIMUM_RETRY);
    ESP_LOGI(TAG, "  LOG UDP HOST:     %s", CONFIG_CATSCALE_LOG_UDP_HOST);
    ESP_LOGI(TAG, "  LOG UDP PORT:     %d", CONFIG_CATSCALE_LOG_UDP_PORT);
    ESP_LOGI(TAG, "  OTA_PORT:         %d", CONFIG_CATSCALE_OTA_PORT);
    ESP_LOGI(TAG, "  INFLUX_ENDPOINT:  %s", CONFIG_CATSCALE_INFLUX_ENDPOINT);
    ESP_LOGI(TAG, "  INFLUX_ORG:       %s", CONFIG_CATSCALE_INFLUX_ORGANIZATION);
    ESP_LOGI(TAG, "  INFLUX_BUCKET:    %s", CONFIG_CATSCALE_INFLUX_BUCKET);
}

static esp_err_t flash_init()
{
    ESP_LOGI(TAG, "flash_init");

    esp_err_t ret = nvs_flash_init();

    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND)
    {
        ESP_LOGE(TAG, "nvs_flash_init failed: %s. Resetting NVS ...", esp_err_to_name(ret));

        ret = nvs_flash_erase();
        if (ret != ESP_OK) {
            ESP_LOGE(TAG, "nvs_flash_erase failed: %s", esp_err_to_name(ret));
            return ret;
        }

        ret = nvs_flash_init();
        if (ret != ESP_OK) {
            ESP_LOGE(TAG, "nvs_flash_init failed: %s", esp_err_to_name(ret));
            return ret;
        }
    }

    return ret;
}
