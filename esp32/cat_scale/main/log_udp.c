#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "log_udp.h"

#include "sdkconfig.h"

#include <stdio.h>
#include <string.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/event_groups.h>
#include <freertos/message_buffer.h>

#include <esp_system.h>
#include <esp_event.h>
#include <esp_log.h>
#include <esp_netif.h>

#include <lwip/err.h>
#include <lwip/sockets.h>
#include <lwip/sys.h>
#include <lwip/netdb.h>
#include <lwip/dns.h>

static const char *TAG = "log_udp";

static vprintf_like_t original_log_function = NULL;
static MessageBufferHandle_t log_message_buffer = NULL;

extern bool g_network_ready;

static int custom_log_function(const char *format, va_list args);
static void log_publish_task();

esp_err_t log_udp_init()
{
    ESP_LOGI(TAG, "log_udp_init");

    log_message_buffer = xMessageBufferCreate(8 * 1024);
    if (!log_message_buffer)
    {
        ESP_LOGE(TAG, "failed to create log message buffer");
        return ESP_FAIL;
    }
    
    original_log_function = esp_log_set_vprintf(custom_log_function);

    ESP_LOGI(TAG, "First message to log buffer");

    xTaskCreate(log_publish_task, "log_publish_task", 8 * 1024, NULL, tskIDLE_PRIORITY + 1, NULL);

    return ESP_OK;
}

static bool string_has_content(const char *str, size_t length)
{
    assert(str);

    for(int i=0; i<length; i++)
    {
        const char c = str[i];

        if ((c >= 'a' && c <= 'z') ||
            (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9'))
        {
            return true;
        }
    }

    return false;
}

static int custom_log_function(const char *format, va_list args)
{
    if (log_message_buffer)
    {
        char buffer[256] = {};
        vsnprintf(buffer, sizeof(buffer), format, args);
        const size_t length = strlen(buffer);

        if (string_has_content(buffer, length))
        {
            BaseType_t pxHigherPriorityTaskWoken = pdFALSE;
            size_t bytes_written = xMessageBufferSendFromISR(log_message_buffer, buffer, length, &pxHigherPriorityTaskWoken);
            if (!bytes_written) ESP_LOGE(TAG, "not enough space in log buffer");
        }
    }

    return original_log_function(format, args);
}

static void log_publish_task()
{
    //const char * const target_hostname = CONFIG_CATSCALE_LOG_UDP_HOST;
    const int target_port = CONFIG_CATSCALE_LOG_UDP_PORT;

    ESP_LOGI(TAG, "log_publish_task");

    while(!g_network_ready) {
        vTaskDelay(1000 / portTICK_PERIOD_MS);
    }

    ESP_LOGI(TAG, "network is ready");

    while(true)
    {
        // TODO what if the ip of the target host changes later?
        // const struct hostent * const he = gethostbyname(target_hostname);
        // if (!he) {
        //     ESP_LOGE(TAG, "gethostbyname(%s) failed", target_hostname);
        //     goto try_again_later;
        // }
        
        // const in_addr_t addr = *(in_addr_t*)he->h_addr_list[0];
        // ESP_LOGI(TAG, "resolved '%s' to '%s'", target_hostname, inet_ntoa(addr));

        struct sockaddr_in dest_addr = {};
        dest_addr.sin_family = AF_INET;
        dest_addr.sin_addr.s_addr = INADDR_BROADCAST; //addr;
        dest_addr.sin_port = htons(target_port);

        const int sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_IP);
        if (sock < 0) {
            ESP_LOGE(TAG, "Unable to create socket: errno %d", errno);
            goto try_again_later;
        }

        while(true)
        {
            char buffer[256] = {};
            const size_t received_bytes = xMessageBufferReceive(log_message_buffer, buffer, sizeof(buffer), portMAX_DELAY); // blocks until message received
            if (!received_bytes) {
                ESP_LOGE(TAG, "did not get any item from message buffer");
                vTaskDelay(1000 / portTICK_PERIOD_MS);
                continue;
            }

            const ssize_t sent_bytes = sendto(sock, buffer, received_bytes, 0, (struct sockaddr *)&dest_addr, sizeof(dest_addr));
            if (sent_bytes < received_bytes) {
                ESP_LOGW(TAG, "sendto failed (sent %d / %u)", sent_bytes, received_bytes);
                // TODO break loop if this happens more than x times?
            }
        }

        close(sock);

        try_again_later:
        ESP_LOGI(TAG, "trying again in 5s");
        vTaskDelay(5000 / portTICK_PERIOD_MS);
    }
}
