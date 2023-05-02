#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "rc.h"
#include "measurement.h"

#include "sdkconfig.h"

#include <stdio.h>
#include <string.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/event_groups.h>

#include <esp_system.h>
#include <esp_event.h>
#include <esp_log.h>
#include <esp_netif.h>

#include <esp_ota_ops.h>
#include <esp_app_desc.h>
#include <esp_app_format.h>
#include <esp_http_client.h>
#include <esp_flash_partitions.h>
#include <esp_partition.h>

#include <lwip/err.h>
#include <lwip/sockets.h>
#include <lwip/sys.h>
#include <lwip/netdb.h>


#define HASH_LEN 32 /* SHA-256 digest length */


static const char *TAG = "rc";

static void rc_ota_task();


static void print_sha256(const uint8_t *image_hash, const char *label)
{
    char hash_print[HASH_LEN * 2 + 1];
    hash_print[HASH_LEN * 2] = 0;
    for (int i = 0; i < HASH_LEN; ++i) {
        sprintf(&hash_print[i * 2], "%02x", image_hash[i]);
    }
    ESP_LOGI(TAG, "%s: %s", label, hash_print);
}

static void print_partition_data(const esp_partition_t *partition, const char *text)
{
    if (partition)
    {
        char buffer[128] = {};
        snprintf(buffer, sizeof(buffer), "type=%X subtype=%X addr=%X size=%X label='%s'",
            partition->type, partition->subtype, partition->address, partition->size, partition->label);
        ESP_LOGI(TAG, "%s: %s", text, buffer);

        esp_ota_img_states_t image_state;
        if (esp_ota_get_state_partition(partition, &image_state) == ESP_OK)
            ESP_LOGI(TAG, "image state %X", image_state);
        else
            ESP_LOGI(TAG, "no image state");

        esp_app_desc_t app_desc = {};
        if (esp_ota_get_partition_description(partition, &app_desc) == ESP_OK)
            ESP_LOGI(TAG, "app desc '%s' '%s'", app_desc.project_name, app_desc.version);
        else
            ESP_LOGI(TAG, "no app desc");
    }
    else
    {
        ESP_LOGI(TAG, "%s: NULL", text);
    }
}

esp_err_t rc_init()
{
    ESP_LOGI(TAG, "rc_init");

    {
        const esp_app_desc_t * const app_desc = esp_app_get_description();

        ESP_LOGI(TAG, "project '%s' version '%s' date '%s' time '%s' ver '%s'",
            app_desc->project_name, app_desc->version, app_desc->date, app_desc->time, app_desc->version);

        print_sha256(app_desc->app_elf_sha256, "app_elf_sha256");
    }

    {
        uint8_t sha_256[HASH_LEN] = { 0 };
        esp_partition_t partition;

        // get sha256 digest for the partition table
        partition.address   = ESP_PARTITION_TABLE_OFFSET;
        partition.size      = ESP_PARTITION_TABLE_MAX_LEN;
        partition.type      = ESP_PARTITION_TYPE_DATA;
        esp_partition_get_sha256(&partition, sha_256);
        print_sha256(sha_256, "SHA-256 for the partition table: ");

        // get sha256 digest for bootloader
        partition.address   = ESP_BOOTLOADER_OFFSET;
        partition.size      = ESP_PARTITION_TABLE_OFFSET;
        partition.type      = ESP_PARTITION_TYPE_APP;
        esp_partition_get_sha256(&partition, sha_256);
        print_sha256(sha_256, "SHA-256 for bootloader: ");

        // get sha256 digest for running partition
        esp_partition_get_sha256(esp_ota_get_running_partition(), sha_256);
        print_sha256(sha_256, "SHA-256 for current firmware: ");

        esp_partition_get_sha256(esp_ota_get_boot_partition(), sha_256);
        print_sha256(sha_256, "SHA-256 for boot firmware: ");
    }

    {
        const esp_partition_t *boot_part = esp_ota_get_boot_partition();
        const esp_partition_t *running_part = esp_ota_get_running_partition();
        const esp_partition_t *next_ota_part = esp_ota_get_next_update_partition(NULL);
        const esp_partition_t *last_invalid_part = esp_ota_get_last_invalid_partition();

        print_partition_data(boot_part, "boot partition");
        print_partition_data(running_part, "running partition");
        print_partition_data(next_ota_part, "next OTA partition");
        print_partition_data(last_invalid_part, "last invalid partition");

        esp_ota_img_states_t running_state;
        if (esp_ota_get_state_partition(running_part, &running_state) == ESP_OK)
            ESP_LOGI(TAG, "running partition state %X", running_state);
        else
            ESP_LOGI(TAG, "can't get running partition state");
    }

    {
        const esp_partition_t *running = esp_ota_get_running_partition();
        esp_ota_img_states_t ota_state;
        if (esp_ota_get_state_partition(running, &ota_state) == ESP_OK) {
            if (ota_state == ESP_OTA_IMG_PENDING_VERIFY) {
                ESP_LOGI(TAG, "Running partition is in state ESP_OTA_IMG_PENDING_VERIFY, cancelling rollback");
                esp_ota_mark_app_valid_cancel_rollback();
            }
        }
    }

    xTaskCreate(rc_ota_task, "rc_ota_task", 8 * 1024, NULL, tskIDLE_PRIORITY + 1, NULL);

    return ESP_OK;
}

#define RC_COMMAND_REBOOT               0x01
#define RC_COMMAND_TEST_START           0x02
#define RC_COMMAND_TEST_STABLE          0x04
#define RC_COMMAND_TEST_END             0x08

typedef struct {
    size_t total_bytes_received;

    bool is_doing_ota;
    esp_ota_handle_t ota_update_handle;
    const esp_partition_t *ota_update_partition;

    uint32_t commands_received;
    uint32_t commands_parameter;

} process_state_t;

static void process_begin(process_state_t *state)
{
    state->total_bytes_received = 0;

    state->is_doing_ota = false;
    state->ota_update_handle = 0;
    state->ota_update_partition = NULL;

    state->commands_received = 0;
    state->commands_parameter = 0;
}

static void process_data(process_state_t *state, void *data, size_t length)
{
    if (state->total_bytes_received == 0)
    {
        ESP_LOGI(TAG, "got %u bytes on first read", length);

        const esp_image_header_t * const image_header = data;
        const esp_app_desc_t * const app_desc = data + sizeof(esp_image_header_t) + sizeof(esp_image_segment_header_t);

        if (image_header->magic == ESP_IMAGE_HEADER_MAGIC &&
            app_desc->magic_word == ESP_APP_DESC_MAGIC_WORD)
        {
            ESP_LOGI(TAG, "project '%s' version '%s' date '%s' time '%s' ver '%s'",
                app_desc->project_name, app_desc->version, app_desc->date, app_desc->time, app_desc->version);
            print_sha256(app_desc->app_elf_sha256, "app_elf_sha256");

            state->ota_update_partition = esp_ota_get_next_update_partition(NULL);

            if (state->ota_update_partition)
            {
                ESP_LOGI(TAG, "OTA update partition: subtype %d at offset 0x%x",
                    state->ota_update_partition->subtype, state->ota_update_partition->address);

                esp_err_t err = esp_ota_begin(state->ota_update_partition, OTA_WITH_SEQUENTIAL_WRITES, &state->ota_update_handle);
                if (err == ESP_OK)
                {
                    ESP_LOGI(TAG, "Starting OTA ...");
                    state->is_doing_ota = true;
                }
                else
                {
                    ESP_LOGE(TAG, "esp_ota_begin failed: %s", esp_err_to_name(err));
                }
            }
            else
            {
                ESP_LOGE(TAG, "esp_ota_get_next_update_partition failed");
            }
        }
        else
        {
            ESP_LOGI(TAG, "did not receive application image (%X,%X)", image_header->magic, app_desc->magic_word);
            ESP_LOG_BUFFER_HEX(TAG, data, length);

            if (strncmp(data, "reboot", 6) == 0)
            {
                ESP_LOGI(TAG, "Reboot command received");
                state->commands_received |= RC_COMMAND_REBOOT;
            }
            else if (strncmp(data, "start", 5) == 0)
            {
                ESP_LOGI(TAG, "Test start command received");
                state->commands_received |= RC_COMMAND_TEST_START;
            }
            else if (strncmp(data, "stable ", 7) == 0)
            {
                ESP_LOGI(TAG, "Test stable command received");
                state->commands_received |= RC_COMMAND_TEST_STABLE;
                state->commands_parameter = (uint32_t)strtol(data+7, NULL, 10);
                ESP_LOGI(TAG, "Parameter %u", state->commands_parameter);
            }
            else if (strncmp(data, "end", 3) == 0)
            {
                ESP_LOGI(TAG, "Test end command received");
                state->commands_received |= RC_COMMAND_TEST_END;
            }
            else
            {
                ESP_LOGI(TAG, "Unknown command");
            }

            // TODO
            // set runtime config ?
            // eg:
            // "set http_addr Framework:5155"

        }
    }

    if (state->is_doing_ota)
    {
        esp_err_t err = esp_ota_write(state->ota_update_handle, data, length);
        if (err != ESP_OK)
        {
            ESP_LOGE(TAG, "esp_ota_write failed: %s (total received %u)", esp_err_to_name(err), state->total_bytes_received);

            ESP_LOGE(TAG, "Aborting OTA ...");
            esp_ota_abort(state->ota_update_handle);

            state->is_doing_ota = false;
            state->ota_update_handle = 0;
            state->ota_update_partition = 0;
        }
    }

    state->total_bytes_received += (size_t)length;
}

static void process_end(process_state_t *state)
{
    ESP_LOGI(TAG, "total received %u", state->total_bytes_received);

    if (state->is_doing_ota)
    {
        ESP_LOGI(TAG, "Finishing OTA ...");

        esp_err_t err = esp_ota_end(state->ota_update_handle);
        if (err != ESP_OK)
        {
            ESP_LOGE(TAG, "esp_ota_end failed: %s", esp_err_to_name(err));
            return;
        }

        err = esp_ota_set_boot_partition(state->ota_update_partition);
        if (err != ESP_OK)
        {
            ESP_LOGE(TAG, "esp_ota_set_boot_partition failed: %s", esp_err_to_name(err));
            return;
        }

        for(int i=3; i>0; i--)
        {
            ESP_LOGI(TAG, "OTA finished. Rebooting in %ds ...", i);
            vTaskDelay(1000 / portTICK_PERIOD_MS);
        }
        esp_restart();
    }

    if (state->commands_received & RC_COMMAND_REBOOT)
    {
        for(int i=3; i>0; i--)
        {
            ESP_LOGI(TAG, "Reboot command received. Rebooting in %ds ...", i);
            vTaskDelay(1000 / portTICK_PERIOD_MS);
        }
        esp_restart();
    }

    if (state->commands_received & RC_COMMAND_TEST_START)
    {
        measurement_mark_start_of_event();
    }

    if (state->commands_received & RC_COMMAND_TEST_STABLE)
    {
        measurement_push_stable_phase(10.0, (double)state->commands_parameter);
    }

    if (state->commands_received & RC_COMMAND_TEST_END)
    {
        measurement_mark_end_of_event();
    }
}

static void rc_ota_task()
{
    ESP_LOGI(TAG, "rc ota task");

    struct sockaddr_storage dest_addr;
    struct sockaddr_in *dest_addr_ip4 = (struct sockaddr_in *)&dest_addr;

    dest_addr_ip4->sin_addr.s_addr = htonl(INADDR_ANY);
    dest_addr_ip4->sin_family = AF_INET;
    dest_addr_ip4->sin_port = htons(CONFIG_CATSCALE_OTA_PORT);

    int listen_sock = socket(AF_INET, SOCK_STREAM, IPPROTO_IP);
    if (listen_sock < 0) {
        ESP_LOGE(TAG, "Unable to create socket: errno %d", errno);
        vTaskDelete(NULL);
        return;
    }

    int opt = 1;
    setsockopt(listen_sock, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt));

    int err = bind(listen_sock, (struct sockaddr *)&dest_addr, sizeof(dest_addr));
    if (err != 0) {
        ESP_LOGE(TAG, "Socket unable to bind: errno %d", errno);
        goto cleanup;
    }

    err = listen(listen_sock, 1);
    if (err != 0) {
        ESP_LOGE(TAG, "Error occurred during listen: errno %d", errno);
        goto cleanup;
    }

    while(1)
    {
        ESP_LOGI(TAG, "Socket listening");

        struct sockaddr_storage source_addr;
        socklen_t addr_len = sizeof(source_addr);

        int sock = accept(listen_sock, (struct sockaddr *)&source_addr, &addr_len);
        if (sock < 0) {
            ESP_LOGE(TAG, "Unable to accept connection: errno %d", errno);
            break;
        }

        // Set tcp keepalive option
        int keepAlive = 1;
        int keepIdle = 5;       // Keep-alive idle time. In idle time without receiving any data from peer, will send keep-alive probe packet
        int keepInterval = 5;   // Keep-alive probe packet interval time.
        int keepCount = 3;      // Keep-alive probe packet retry count.

        setsockopt(sock, SOL_SOCKET, SO_KEEPALIVE, &keepAlive, sizeof(int));
        setsockopt(sock, IPPROTO_TCP, TCP_KEEPIDLE, &keepIdle, sizeof(int));
        setsockopt(sock, IPPROTO_TCP, TCP_KEEPINTVL, &keepInterval, sizeof(int));
        setsockopt(sock, IPPROTO_TCP, TCP_KEEPCNT, &keepCount, sizeof(int));

        // Convert ip address to string
        if (source_addr.ss_family == PF_INET)
        {
            char addr_str[128];
            inet_ntoa_r(((struct sockaddr_in *)&source_addr)->sin_addr, addr_str, sizeof(addr_str) - 1);
            ESP_LOGI(TAG, "Socket accepted ip address: %s", addr_str);
        }
        else
        {
            ESP_LOGI(TAG, "Invalid ss_family %x", source_addr.ss_family);
        }

        send(sock, "hello", 5, 0);

        process_state_t state = {};
        process_begin(&state);

        const size_t receive_buffer_size = 1024;
        void * const receive_buffer = malloc(receive_buffer_size);
        assert(receive_buffer);

        while(1)
        {
            int received_bytes = recv(sock, receive_buffer, receive_buffer_size, 0);
            if (received_bytes < 0)
            {
                ESP_LOGI(TAG, "Error while receiving: errno %d", errno);
                break;
            }
            else if (received_bytes == 0) // connection closed or no more data?
            {
                break;
            }
            else
            {
                assert(received_bytes > 0);
                process_data(&state, receive_buffer, (size_t)received_bytes);
            }
        }

        free(receive_buffer);

        send(sock, "bye", 3, 0);

        shutdown(sock, 0);
        close(sock);

        process_end(&state);
    }

cleanup:
    ESP_LOGI(TAG, "rc ota task cleanup");
    close(listen_sock);
    vTaskDelete(NULL);
}
