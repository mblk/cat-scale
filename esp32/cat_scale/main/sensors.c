
#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "sensors.h"
#include "hx711.h"
#include "bme280_user.h"
#include "ccs811.h"
#include "time.h"
#include "http.h"
#include "measurement.h"

#include "sdkconfig.h"

#include <stdio.h>
#include <string.h>
#include <math.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/message_buffer.h>

#include <esp_system.h>
#include <esp_log.h>
#include <esp_timer.h>

#include <driver/i2c.h>

static const char *TAG = "sensors";

typedef struct sensor_data {
    struct timeval time;

    uint32_t weight_raw;
    double weight;
    int measurement_state;

    double temperature;
    double pressure;
    double humidity;

    uint32_t co2;
    uint32_t tvoc;
} sensor_data_t;

#define SENSOR_AVERAGE_COUNT    5       // 10Hz / 5 = 2Hz
#define SENSOR_RINGBUFFER_SIZE  128     // Number of items. 128/2Hz -> 64s

static portMUX_TYPE sensor_ringbuffer_spinlock = portMUX_INITIALIZER_UNLOCKED;
static sensor_data_t sensor_ringbuffer[SENSOR_RINGBUFFER_SIZE] = {0};
static size_t sensor_ringbuffer_write_index = 0;
static size_t sensor_ringbuffer_read_index = 0;

static esp_err_t i2c_master_init();
static void sensors_read_task();
static void sensors_post_task();

esp_err_t sensors_init()
{
    esp_log_level_set(TAG, ESP_LOG_DEBUG);
    ESP_LOGI(TAG, "sensors_init");

    // GPIO sensors.
    ESP_ERROR_CHECK(hx711_init());
    
    // I2C sensors.
    ESP_ERROR_CHECK(i2c_master_init());
    ESP_ERROR_CHECK(bme280_user_init());
    ESP_ERROR_CHECK(ccs811_init());

    xTaskCreate(sensors_read_task, "sensors_read_task", 8 * 1024, NULL, tskIDLE_PRIORITY + 2, NULL);
    xTaskCreate(sensors_post_task, "sensors_post_task", 8 * 1024, NULL, tskIDLE_PRIORITY + 1, NULL);

    return ESP_OK;
}

static esp_err_t i2c_master_init()
{
    ESP_LOGI(TAG, "i2c master init ...");

    // *** config ***
    const i2c_port_t i2c_master_port = 0;
    const uint32_t i2c_clock_speed = 100000; // max is 400kHz but ccs811 doesn't work above 100kHz for some reason :/
    const int scl_io_num = GPIO_NUM_4;
    const int sda_io_num = GPIO_NUM_5;
    // *** config ***

    const i2c_config_t conf = {
        .mode = I2C_MODE_MASTER,
        .sda_io_num = sda_io_num,
        .scl_io_num = scl_io_num,
        .sda_pullup_en = GPIO_PULLUP_ENABLE,
        .scl_pullup_en = GPIO_PULLUP_ENABLE,
        .master.clk_speed = i2c_clock_speed,
    };

    esp_err_t err = i2c_param_config(i2c_master_port, &conf);
    if (err != ESP_OK) {
        return err;
    }

    return i2c_driver_install(i2c_master_port, conf.mode, 0, 0, 0);
}

static void sensor_ringbuffer_push(const sensor_data_t *input)
{
    assert(input);
    bool overflow = false;

    taskENTER_CRITICAL(&sensor_ringbuffer_spinlock);
    {
        sensor_data_t * const item = sensor_ringbuffer + sensor_ringbuffer_write_index;
        memcpy(item, input, sizeof(sensor_data_t));
        sensor_ringbuffer_write_index = (sensor_ringbuffer_write_index + 1) % SENSOR_RINGBUFFER_SIZE;
        overflow = sensor_ringbuffer_write_index == sensor_ringbuffer_read_index;
    }
    taskEXIT_CRITICAL(&sensor_ringbuffer_spinlock);

    if (overflow) ESP_LOGE(TAG, "sensor ring buffer overflow");
}

static bool sensor_ringbuffer_pop(sensor_data_t *output)
{
    assert(output);
    bool ret = false;

    taskENTER_CRITICAL(&sensor_ringbuffer_spinlock);
    {
        // not empty?
        if (sensor_ringbuffer_write_index != sensor_ringbuffer_read_index)
        {
            const sensor_data_t * const item = sensor_ringbuffer + sensor_ringbuffer_read_index;
            memcpy(output, item, sizeof(sensor_data_t));
            sensor_ringbuffer_read_index = (sensor_ringbuffer_read_index + 1) % SENSOR_RINGBUFFER_SIZE;
            ret = true;
        }
    }
    taskEXIT_CRITICAL(&sensor_ringbuffer_spinlock);

    return ret;
}

static esp_err_t read_data_from_sensors(sensor_data_t *sensor_data)
{
    assert(sensor_data);
    memset(sensor_data, 0, sizeof(sensor_data_t));

    // time
    gettimeofday(&sensor_data->time, NULL);

    // weight
    sensor_data->weight_raw = hx711_read_from_sensor();
    if (sensor_data->weight_raw == 0)
    {
        ESP_LOGD(TAG, "failed to read data from hx711");
        return ESP_FAIL;
    }

    // temperature, pressure, humidity
    esp_err_t ret = bme280_user_read_from_sensor(&sensor_data->temperature, &sensor_data->pressure, &sensor_data->humidity);
    if (ret != ESP_OK)
    {
        ESP_LOGD(TAG, "failed to read data from bme280");
        return ESP_FAIL;
    }

    // co2, tvoc
    ret = ccs811_get_latest_values(&sensor_data->co2, &sensor_data->tvoc);
    if (ret != ESP_OK)
    {
        ESP_LOGD(TAG, "failed to read data from ccs811");
        return ESP_FAIL;
    }

    return ESP_OK;
}

static void calculate_average_sensor_data(const sensor_data_t *input, size_t input_count, sensor_data_t *output)
{
    sensor_data_t sum = {};

    for(size_t i=0; i<input_count; i++)
    {
        sum.weight_raw  += input[i].weight_raw;
        sum.temperature += input[i].temperature;
        sum.pressure    += input[i].pressure;
        sum.humidity    += input[i].humidity;
        sum.co2         += input[i].co2;
        sum.tvoc        += input[i].tvoc;
    }

    const double avg_count = (double)input_count;

    sensor_data_t avg =
    {
        .time        = input[0].time, // TODO

        .weight_raw  = round((double)sum.weight_raw  / avg_count),
        .temperature =               sum.temperature / avg_count,
        .pressure    =               sum.pressure    / avg_count,
        .humidity    =               sum.humidity    / avg_count,
        .co2         = round((double)sum.co2         / avg_count),
        .tvoc        = round((double)sum.tvoc        / avg_count),
    };

    // Find 1 outlier in weight_raw as the signal is very noisy.
    uint32_t max_weight_diff = 0;
    size_t max_weight_diff_index = 0;

    for(size_t i=0; i<input_count; i++)
    {
        uint32_t weight_diff = abs((int32_t)avg.weight_raw - (int32_t)input[i].weight_raw);
        if (weight_diff > max_weight_diff)
        {
            max_weight_diff = weight_diff;
            max_weight_diff_index = i;
        }
    }

    // Re-calculate average weight without the outlier.
    sum.weight_raw = 0;
    for(int i=0; i<input_count; i++)
        if (i != max_weight_diff_index)
            sum.weight_raw += input[i].weight_raw;

    avg.weight_raw = round((double)sum.weight_raw / (avg_count - 1.0));

    memcpy(output, &avg, sizeof(sensor_data_t));
}

static void sensors_read_task()
{
    ESP_LOGI(TAG, "sensors_read_task");
    
    while(true)
    {
        // Sampling rates:
        // hx711:  1 / 100ms    (blocking)
        // bme280: 1 / 62.5ms   (blocking)
        // ccs811: 1 / 1s       (implementation does not block)

        sensor_data_t sensor_data[SENSOR_AVERAGE_COUNT] = {};
        for (size_t i=0; i<SENSOR_AVERAGE_COUNT; i++)
        {
            vTaskDelay(80 / portTICK_PERIOD_MS); // target is 100ms
            read_data_from_sensors(sensor_data + i);
        }

        sensor_data_t avg_sensor_data = {};
        calculate_average_sensor_data(sensor_data, SENSOR_AVERAGE_COUNT, &avg_sensor_data);

        // The ccs811 sensor needs to know the external temperature and humidity to perform some corrections.
        ccs811_set_environment_data(avg_sensor_data.temperature, avg_sensor_data.humidity);

        avg_sensor_data.weight = measurement_process_raw_weight(avg_sensor_data.weight_raw);
        avg_sensor_data.measurement_state = measurement_process_corrected_weight(avg_sensor_data.weight);

        sensor_ringbuffer_push(&avg_sensor_data);

        // TODO freq messen und ausgeben?
        //esp_timer_get_time() // us
    }
}

static size_t create_sensor_data_line_protocol(char *message_buffer, size_t message_buffer_size)
{
    assert(message_buffer);
    assert(message_buffer_size);
    
    memset(message_buffer, 0, message_buffer_size);

    size_t message_buffer_offset = 0;
    size_t data_count = 0;

    while(true)
    {
        size_t free_space = message_buffer_size - message_buffer_offset;
        if (free_space < 256)
        {
            ESP_LOGE(TAG, "http message buffer is full");
            break;
        }

        sensor_data_t data = {};
        bool got_data = sensor_ringbuffer_pop(&data);
        if (!got_data) break;

        uint64_t timestamp = ((data.time.tv_sec * 1000 * 1000) + data.time.tv_usec) * 1000; // unix-time in ns

        char item_buffer[256] = {};
        size_t item_buffer_length = snprintf(item_buffer, sizeof(item_buffer),
            "scales,scale_id=CAT1 weight_raw=%u,weight=%0.3f,mstate=%d,temperature=%0.3f,humidity=%0.3f,pressure=%0.3f,co2=%u,tvoc=%u %"PRIu64"\n",
            data.weight_raw, data.weight, data.measurement_state, data.temperature, data.humidity, data.pressure, data.co2, data.tvoc, timestamp);
        
        strcpy(message_buffer + message_buffer_offset, item_buffer);
        message_buffer_offset += item_buffer_length;
        data_count++;
    }

    return data_count;
}

static void sensors_post_task()
{
    ESP_LOGI(TAG, "sensors_post_task");

    const size_t message_buffer_size = 16 * 1024;
    char * const message_buffer = malloc(message_buffer_size);
    assert(message_buffer);

    while(true)
    {
        vTaskDelay(10 * 1000 / portTICK_PERIOD_MS);

        size_t sensor_data_count = create_sensor_data_line_protocol(message_buffer, message_buffer_size);
        ESP_LOGI(TAG, "posting %u items ...", sensor_data_count);
        if (sensor_data_count)
        {
            esp_err_t ret = http_post_sensor_data_influx(message_buffer);
            if (ret != ESP_OK)
            {
                ESP_LOGE(TAG, "failed to post sensor data");
            }
        }
    }
}
