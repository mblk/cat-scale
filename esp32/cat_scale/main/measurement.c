#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "measurement.h"
#include "time.h"
#include "http.h"

#include "sdkconfig.h"

#include <stdio.h>
#include <string.h>
#include <math.h>
#include <stdbool.h>
#include <stdint.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/message_buffer.h>

#include <esp_system.h>
#include <esp_log.h>

static const char *TAG = "measurement";

enum event_type {
    event_type_start_of_event,
    event_type_stable_phase,
    event_type_end_of_event
};

typedef struct {
    const uint32_t event_type;
    const struct timeval timestamp;
    union {
        struct {
            const double length;
            const double value;
        } stable_phase;
        // possibly other data later ...
     };
} event_t;

typedef struct stable_phase {
    const struct timeval timestamp;
    const double length;
    const double value;
    struct stable_phase *next;
} stable_phase_t;

typedef struct {
    const struct timeval start;
    struct timeval end;
    stable_phase_t *first_stable_phase;
    stable_phase_t *last_stable_phase;
} scale_event_t;

static MessageBufferHandle_t event_message_buffer = NULL;

static void measurement_post_task(void*);

esp_err_t measurement_init(void)
{
    ESP_LOGI(TAG, "measurement_init");

    event_message_buffer = xMessageBufferCreate(1024);
    assert(event_message_buffer);

    xTaskCreate(measurement_post_task, "measurement_post_task", 8 * 1024, NULL, tskIDLE_PRIORITY + 1, NULL);

    return ESP_OK;
}

static scale_event_t *create_scale_event(const event_t *event)
{
    assert(event);

    const scale_event_t init = {
        .start = event->timestamp,
    };

    scale_event_t *scale_event = malloc(sizeof(scale_event_t));
    assert(scale_event);
    memcpy(scale_event, &init, sizeof(scale_event_t));

    return scale_event;
}

static void finish_scale_event(scale_event_t *scale_event, const event_t *event)
{
    assert(scale_event);
    assert(event);

    scale_event->end = event->timestamp;
}

static void destroy_scale_event(scale_event_t *scale_event)
{
    assert(scale_event);

    stable_phase_t *current = scale_event->first_stable_phase;
    while(current) {
        stable_phase_t *next = current->next;
        free(current);
        current = next;
    }

    free(scale_event);
}

static void add_stable_phase(scale_event_t *scale_event, const event_t *event)
{
    assert(scale_event);
    assert(event);

    const stable_phase_t init = {
        .timestamp = event->timestamp,
        .length = event->stable_phase.length,
        .value = event->stable_phase.value,
        .next = NULL,
    };

    stable_phase_t *stable_phase = malloc(sizeof(stable_phase_t));
    assert(stable_phase);
    memcpy(stable_phase, &init, sizeof(stable_phase_t));

    if (scale_event->first_stable_phase == NULL || scale_event->last_stable_phase == NULL) {
        scale_event->first_stable_phase = stable_phase;
        scale_event->last_stable_phase = stable_phase;
    } else {
        scale_event->last_stable_phase->next = stable_phase;
        scale_event->last_stable_phase = stable_phase;
    }
}

static esp_err_t serialize_scale_event(const scale_event_t *scale_event, char *output_buffer, size_t output_buffer_size)
{
    assert(scale_event);
    memset(output_buffer, 0, output_buffer_size);
    size_t written_characters = 0;

    const int toiletId = 1; // TODO from config?

    char start_time_buffer[32] = {};
    char end_time_buffer[32] = {};
    convert_timeval_to_iso8601(scale_event->start, start_time_buffer, sizeof(start_time_buffer));
    convert_timeval_to_iso8601(scale_event->end, end_time_buffer, sizeof(end_time_buffer));

    // prefix
    written_characters += snprintf(
        output_buffer + written_characters,
        output_buffer_size - written_characters,
        "{"
        "\"toiletId\": %d,"
        "\"startTime\": \"%s\","
        "\"endTime\": \"%s\","
        "\"stablePhases\": [",
        toiletId,
        start_time_buffer,
        end_time_buffer
        );

    if (written_characters >= output_buffer_size) {
        ESP_LOGE(TAG, "buffer full #1");
        return ESP_ERR_NO_MEM;
    }

    // stable phases
    stable_phase_t *stable_phase = scale_event->first_stable_phase;
    while(stable_phase) {

        char time_buffer[32] = {};
        convert_timeval_to_iso8601(stable_phase->timestamp, time_buffer, sizeof(time_buffer));

        written_characters += snprintf(
            output_buffer + written_characters,
            output_buffer_size - written_characters,
            "{"
            "\"timestamp\": \"%s\","
            "\"length\": %0.1f,"
            "\"value\": %0.1f"
            "}%s",
            time_buffer,
            stable_phase->length,
            stable_phase->value,
            stable_phase->next ? "," : ""
        );

        if (written_characters >= output_buffer_size) {
            ESP_LOGE(TAG, "buffer full #2");
            return ESP_ERR_NO_MEM;
        }

        stable_phase = stable_phase->next;
    }

    // postfix
    written_characters += snprintf(
        output_buffer + written_characters,
        output_buffer_size - written_characters,
        "]}");

    if (written_characters >= output_buffer_size) {
        ESP_LOGE(TAG, "buffer full #3");
        return ESP_ERR_NO_MEM;
    }

    return ESP_OK;
}

static void measurement_post_task(void*)
{
    ESP_LOGI(TAG, "measurement_post_task");

    scale_event_t *current_event = NULL;

    const size_t message_buffer_size = 4 * 1024;
    char *message_buffer = malloc(message_buffer_size);
    assert(message_buffer);
    memset(message_buffer, 0, message_buffer_size);

    while(true)
    {
        event_t e = {};
        size_t bytes_read = xMessageBufferReceive(event_message_buffer, &e, sizeof(event_t), portMAX_DELAY); // blocking read
        if (bytes_read == sizeof(event_t))
        {
            switch (e.event_type)
            {
                case event_type_start_of_event:
                    ESP_LOGI(TAG, "Post task: received start of event");
                    if (current_event)
                        destroy_scale_event(current_event);
                    current_event = create_scale_event(&e);
                    break;

                case event_type_stable_phase:
                    ESP_LOGI(TAG, "Post task: received stable phase");
                    if (current_event)
                        add_stable_phase(current_event, &e);
                    break;

                case event_type_end_of_event:
                    ESP_LOGI(TAG, "Post task: received end of event");
                    if (current_event)
                    {
                        finish_scale_event(current_event, &e);

                        ESP_LOGI(TAG, "Posting scale event data (%zu bytes) ...", strlen(message_buffer));
                        esp_err_t ret = serialize_scale_event(current_event, message_buffer, message_buffer_size);
                        if (ret == ESP_OK) {
                            ret = http_post_json_data("api/ScaleEvent/Create", message_buffer);
                            if (ret != ESP_OK) {
                                ESP_LOGE(TAG, "Failed to post scale event data");
                            }
                        } else {
                            ESP_LOGE(TAG, "Failed to serialize scale event");
                        }

                        destroy_scale_event(current_event);
                        current_event = NULL;
                    }
                    break;

                default:
                    ESP_LOGI(TAG, "Post task: received unknown event");
                    break;
            }
        }
    }
}

static void send_event(const event_t *event)
{
    assert(event);
    size_t bytes_written = xMessageBufferSend(event_message_buffer, event, sizeof(event_t), 0);
    if (bytes_written != sizeof(event_t))
        ESP_LOGE(TAG, "Failed to add event to buffer");
}

void measurement_mark_start_of_event(void)
{
    ESP_LOGI(TAG, "measurement_mark_start_of_event");

    struct timeval tv;
    gettimeofday(&tv, NULL);

    const event_t e = {
        .event_type = event_type_start_of_event,
        .timestamp = tv,
    };

    send_event(&e);
}

void measurement_push_stable_phase(double length, double value)
{
    ESP_LOGI(TAG, "measurement_push_stable_phase %0.1f %0.1f", length, value);

    struct timeval tv;
    gettimeofday(&tv, NULL);

    const event_t e = {
        .event_type = event_type_stable_phase,
        .timestamp = tv,
        .stable_phase.length = length,
        .stable_phase.value = value,
    };

    send_event(&e);
}

void measurement_mark_end_of_event(void)
{
    ESP_LOGI(TAG, "measurement_mark_end_of_event");

    struct timeval tv;
    gettimeofday(&tv, NULL);

    const event_t e = {
        .event_type = event_type_end_of_event,
        .timestamp = tv,
    };

    send_event(&e);
}
