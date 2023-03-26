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

enum event_type
{
    event_type_measurement,
    event_type_cleaning
};

typedef struct
{
    uint32_t event_type;
    union {
        struct {
            double setup_time;
            double poo_time;
            double cleanup_time;
            double cat_weight;
            double poo_weight;
        } m;
        struct {
            double cleaning_time;
            double cleaning_weight;
        } c;
    };
} event_t;

enum measurement_state
{
    state_wait_for_idle = 0,
    state_idle = 1,
    state_setup = 2,
    state_poo = 3,
    state_cleanup = 4,
    state_maintenance = 5,
};

#define ZERO_DETECTION_TICKS        (2 * 30)        // 30s
#define ZERO_DETECTION_THRESHOLD    (23 * 100)      // ~100g
#define WEIGHT_HISTORY_SAMPLES      (2 * 5)         // 5s
#define WEIGHT_SAMPLE_DT            (0.5)           // 2Hz, 0.5s

static MessageBufferHandle_t event_message_buffer = NULL;

// zeroing
static uint32_t raw_weight_prev_values[ZERO_DETECTION_TICKS] = {};
static size_t raw_weight_prev_value_index = 0;
static uint32_t raw_weight_zero_offset = 0;
static bool weight_zeroing_enabled = true;

// measurement state machine
static double weight_history_values[WEIGHT_HISTORY_SAMPLES] = {};
static size_t weight_history_index = 0;
static int measure_state = 0;
static int ticks_in_current_state = 0;
static int plausible_maintenance_ticks = 0;
static event_t current_event = {};

static void measurement_post_task();

esp_err_t measurement_init()
{
    esp_log_level_set(TAG, ESP_LOG_DEBUG);
    ESP_LOGI(TAG, "measurement_init");

    event_message_buffer = xMessageBufferCreate(1024);
    assert(event_message_buffer);

    xTaskCreate(measurement_post_task, "measurement_post_task", 8 * 1024, NULL, tskIDLE_PRIORITY + 1, NULL);

    return ESP_OK;
}

static size_t create_measurement_json(char *message_buffer, size_t message_buffer_size, const event_t *event)
{
    struct timeval tv;
    gettimeofday(&tv, NULL);

    char time_buffer[32] = {};
    convert_timeval_to_iso8601(tv, time_buffer, sizeof(time_buffer));

    return snprintf(message_buffer, message_buffer_size,
        "{"
        "\"toiletId\":1," // TODO from config?
        "\"timeStamp\":\"%s\","
        "\"setupTime\":%0.3f,"
        "\"pooTime\":%0.3f,"
        "\"cleanupTime\":%0.3f,"
        "\"catWeight\":%0.3f,"
        "\"pooWeight\":%0.3f"
        "}",
        time_buffer,
        event->m.setup_time,
        event->m.poo_time,
        event->m.cleanup_time,
        event->m.cat_weight,
        event->m.poo_weight);
}

static size_t create_cleaning_json(char *message_buffer, size_t message_buffer_size, const event_t *event)
{
    struct timeval tv;
    gettimeofday(&tv, NULL);

    char time_buffer[32] = {};
    convert_timeval_to_iso8601(tv, time_buffer, sizeof(time_buffer));

    return snprintf(message_buffer, message_buffer_size,
        "{"
        "\"toiletId\":1," // TODO from config?
        "\"timeStamp\":\"%s\","
        "\"cleaningTime\":%0.3f,"
        "\"cleaningWeight\":%0.3f"
        "}",
        time_buffer,
        event->c.cleaning_time,
        event->c.cleaning_weight);
}

static void measurement_post_task()
{
    ESP_LOGI(TAG, "measurement_post_task");

    const size_t message_buffer_size = 1024;
    char * const message_buffer = malloc(message_buffer_size);
    assert(message_buffer);

    while(true)
    {
        event_t e = {};
        size_t bytes_read = xMessageBufferReceive(event_message_buffer, &e, sizeof(event_t), portMAX_DELAY); // blocking read
        if (bytes_read == sizeof(event_t))
        {
            switch (e.event_type)
            {
                case event_type_measurement:
                    create_measurement_json(message_buffer, message_buffer_size, &e);
                    http_post_json_data("Measurement", message_buffer);
                    break;

                case event_type_cleaning:
                    create_cleaning_json(message_buffer, message_buffer_size, &e);
                    http_post_json_data("Cleaning", message_buffer);
                    break;
            }
        }
        else
        {
            ESP_LOGE(TAG, "Incomplete read from measurement buffer (got %u/%u)", bytes_read, sizeof(event_t));
        }
    }
}

static void calculate_spread_avg_uint32(const uint32_t *values, size_t count, uint32_t *spread, uint32_t *avg)
{
    uint32_t min = values[0];
    uint32_t max = values[0];
    uint64_t sum = 0;

    for (size_t i = 0; i < count; i++)
    {
        uint32_t v = values[i];
        sum += v;
        if (v < min)
            min = v;
        if (v > max)
            max = v;
    }

    *spread = max - min;
    *avg = (uint32_t)round((double)sum / (double)count);
}

static void calculate_spread_avg_double(const double *values, size_t count, double *spread, double *avg)
{
    double min = values[0];
    double max = values[0];
    double sum = 0;

    for (size_t i = 0; i < count; i++)
    {
        double v = values[i];
        sum += v;
        if (v < min)
            min = v;
        if (v > max)
            max = v;
    }

    *spread = max - min;
    *avg = sum / (double)count;
}

double measurement_process_raw_weight(uint32_t weight_raw) // called at 2 Hz
{
    // first time?
    if (raw_weight_zero_offset == 0)
    {
        raw_weight_zero_offset = weight_raw;
        for (size_t i = 0; i < ZERO_DETECTION_TICKS; i++)
            raw_weight_prev_values[i] = weight_raw;
    }

    // store values in ring-buffer
    raw_weight_prev_values[raw_weight_prev_value_index] = weight_raw;
    raw_weight_prev_value_index = (raw_weight_prev_value_index + 1) % ZERO_DETECTION_TICKS;

    // inspect signal
    uint32_t spread = 0, avg = 0;
    calculate_spread_avg_uint32(raw_weight_prev_values, ZERO_DETECTION_TICKS, &spread, &avg);
    ESP_LOGD(TAG, "raw spread over %u samples was %u", ZERO_DETECTION_TICKS, spread);

    if (spread < ZERO_DETECTION_THRESHOLD && weight_zeroing_enabled) // TODO dynamic threshold?
    {
        raw_weight_zero_offset = avg;
        // ESP_LOGI(TAG, "new zero offset %u", raw_weight_zero_offset);
    }

    const int32_t weight_raw_zeroed = (int32_t)weight_raw - (int32_t)raw_weight_zero_offset;

    // convert raw value to grams
    const uint32_t v_zero  = 8612000;
    const uint32_t v_calib = 8825500;
    const uint32_t m_zero  = 0;
    const uint32_t m_calib = 9800; // g
    const uint32_t dv = v_calib - v_zero;
    const uint32_t dm = m_calib - m_zero;
    const double v_per_m = (double)dv / (double)dm; // change in value per gram

    const double weight = (double)weight_raw_zeroed / v_per_m;

    return weight;
}

static void push_measurement()
{
    ESP_LOGI(TAG, "push_measurement setup=%0.1f poo=%0.1f cleanup=%0.1f cat_weight=%0.1f poo_weight=%0.1f",
             current_event.m.setup_time, current_event.m.poo_time, current_event.m.cleanup_time,
             current_event.m.cat_weight, current_event.m.poo_weight);

    current_event.event_type = event_type_measurement;

    size_t bytes_written = xMessageBufferSend(event_message_buffer, &current_event, sizeof(event_t), 0);
    if (bytes_written != sizeof(event_t))
    {
        ESP_LOGE(TAG, "Failed to add measurement to buffer");
    }
}

static void push_cleaning()
{
    ESP_LOGI(TAG, "push_cleaning cleaning_time=%0.1f cleaning_weight=%0.1f",
            current_event.c.cleaning_time, current_event.c.cleaning_weight);

    current_event.event_type = event_type_cleaning;

    size_t bytes_written = xMessageBufferSend(event_message_buffer, &current_event, sizeof(event_t), 0);
    if (bytes_written != sizeof(event_t))
    {
        ESP_LOGE(TAG, "Failed to add cleaning to buffer");
    }
}

static void enter_state(int new_state)
{
    ESP_LOGD(TAG, "state change %d -> %d", measure_state, new_state);
    measure_state = new_state;
    ticks_in_current_state = 0;
}

int measurement_process_corrected_weight(double weight) // called at 2 Hz
{
    // store values in ring-buffer
    weight_history_values[weight_history_index] = weight;
    weight_history_index = (weight_history_index + 1) % WEIGHT_HISTORY_SAMPLES;

    // inspect signal
    double spread = 0, avg = 0;
    calculate_spread_avg_double(weight_history_values, WEIGHT_HISTORY_SAMPLES, &spread, &avg);

    const bool is_stable                = spread < 15.0; // TODO dynamic based on recent noise levels?
    const bool is_zero                  = avg < 10.0;
    const bool is_plausible_cat         = 1000.0 < avg && avg < 10000.0;
    const bool is_plausible_maintenance = avg < -500.0;

    ESP_LOGD(TAG, "state=%d, weight=%0.1f, is_stable=%s, spread %0.1f, avg %0.1f", measure_state, weight, is_stable ? "yes" : "no", spread, avg);

    // enter maintenance?
    if (is_plausible_maintenance)
    {
        plausible_maintenance_ticks++;
        if (plausible_maintenance_ticks > 5)
        {
            memset(&current_event, 0, sizeof(event_t));
            enter_state(state_maintenance);
        }
    }
    else
    {
        plausible_maintenance_ticks = 0;
    }

    // update state machine
    switch (measure_state)
    {
        case state_wait_for_idle: // wait for zero
        {
            if (is_stable && is_zero)
            {
                enter_state(state_idle);
            }
            break;
        }

        case state_idle: // wait for significant weight
        {
            if (is_plausible_cat)
            {
                memset(&current_event, 0, sizeof(event_t));
                enter_state(state_setup);
            }
            break;
        }

        case state_setup: // wait for stable signal
        {
            current_event.m.setup_time += WEIGHT_SAMPLE_DT;

            if (is_stable && is_plausible_cat)
            {
                current_event.m.setup_time -= WEIGHT_HISTORY_SAMPLES * WEIGHT_SAMPLE_DT;
                current_event.m.poo_time += WEIGHT_HISTORY_SAMPLES * WEIGHT_SAMPLE_DT;
                current_event.m.cat_weight = avg;
                enter_state(state_poo);
            }
            break;
        }

        case state_poo: // wait for end of stable signal
        {
            current_event.m.poo_time += WEIGHT_SAMPLE_DT;

            if (!is_stable)
            {
                enter_state(state_cleanup);
            }
            break;
        }

        case state_cleanup: // wait for cat to leave
        {
            current_event.m.cleanup_time += WEIGHT_SAMPLE_DT;

            if (is_stable && !is_plausible_cat)
            {
                current_event.m.poo_weight = avg;
                push_measurement();
                enter_state(state_wait_for_idle);
            }
            break;
        }

        case state_maintenance: // maintenance (ie. cleaning or refilling)
        {
            current_event.c.cleaning_time += WEIGHT_SAMPLE_DT;

            if (is_stable && !is_plausible_maintenance && current_event.c.cleaning_time > 30.0)
            {
                current_event.c.cleaning_weight = avg;
                push_cleaning();
                enter_state(state_wait_for_idle);
            }
            break;
        }

        default:
        {
            ESP_LOGE(TAG, "invalid state %d", measure_state);
            enter_state(state_wait_for_idle);
            break;
        }
    }

    // timeout?
    ticks_in_current_state++;
    if (measure_state > state_idle && ticks_in_current_state > 60*2 * 5) // 5min
    {
        ESP_LOGE(TAG, "Timeout in state %d", measure_state);
        enter_state(state_wait_for_idle);
    }

    weight_zeroing_enabled = (measure_state == state_wait_for_idle || measure_state == state_idle);

    return measure_state;
}

void measurement_test_measurement()
{
    const event_t e = {
        .event_type = event_type_measurement,
        .m.setup_time = 1,
        .m.poo_time = 2,
        .m.cleanup_time = 3,
        .m.cat_weight = 3000,
        .m.poo_weight = 100
    };

    size_t bytes_written = xMessageBufferSend(event_message_buffer, &e, sizeof(event_t), 0);
    if (bytes_written != sizeof(event_t))
        ESP_LOGE(TAG, "Failed to add measurement to buffer");
}

void measurement_test_cleaning()
{
    const event_t e = {
        .event_type = event_type_cleaning,
        .c.cleaning_time = 10,
        .c.cleaning_weight = -100
    };

    size_t bytes_written = xMessageBufferSend(event_message_buffer, &e, sizeof(event_t), 0);
    if (bytes_written != sizeof(event_t))
        ESP_LOGE(TAG, "Failed to add cleaning to buffer");
}
