
#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "sensors.h"
#include "hx711.h"
#include "bme280_user.h"
#include "ccs811.h"
#include "time.h"
#include "http.h"
#include "measurement.h"
#include "filters.h"
#include "filter_cascade.h"
#include "ringbuffer.h"

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

typedef struct {
    uint64_t timestamp; // unix-time in ns
    double weight_raw;
    double weight;
} fast_sensor_data_t;

typedef struct {
    uint64_t timestamp; // unix-time in ns
    double temperature;
    double pressure;
    double humidity;
    uint32_t co2;
    uint32_t tvoc;
} slow_sensor_data_t;

static_assert(sizeof(fast_sensor_data_t) == 24);
static_assert(sizeof(slow_sensor_data_t) == 40);

static ringbuffer_t *sensor_ringbuffer_fast_data = NULL;
static ringbuffer_t *sensor_ringbuffer_slow_data = NULL;

static esp_err_t i2c_master_init(void);
static void sensors_read_task(void*);
static void sensors_post_task(void*);

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

    filter_cascade_init();

    sensor_ringbuffer_fast_data = ringbuffer_create(sizeof(fast_sensor_data_t), 10 * 60);
    sensor_ringbuffer_slow_data = ringbuffer_create(sizeof(slow_sensor_data_t), 1 * 60);

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

static uint64_t get_unix_timestamp_in_ns()
{
    struct timeval tv = {};
    gettimeofday(&tv, NULL);

    uint64_t timestamp = ((tv.tv_sec * 1000 * 1000) + tv.tv_usec) * 1000;

    return timestamp;
}

static esp_err_t read_fast_data_from_sensors(fast_sensor_data_t *sensor_data, double dt)
{
    assert(sensor_data);
    memset(sensor_data, 0, sizeof(fast_sensor_data_t));

    // time
    sensor_data->timestamp = get_unix_timestamp_in_ns();

    // weight
    uint32_t hx711_data = hx711_read_from_sensor();
    if (hx711_data == 0)
    {
        ESP_LOGD(TAG, "failed to read data from hx711");
        return ESP_FAIL;
    }

    sensor_data->weight_raw = (double)hx711_data;
    sensor_data->weight = filter_cascade_process(sensor_data->weight_raw, dt);

    return ESP_OK;
}

static esp_err_t read_slow_data_from_sensors(slow_sensor_data_t *sensor_data)
{
    assert(sensor_data);
    memset(sensor_data, 0, sizeof(slow_sensor_data_t));

    // time
    sensor_data->timestamp = get_unix_timestamp_in_ns();

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

static void sensors_read_task(void *task_args)
{
    ESP_LOGI(TAG, "sensors_read_task");
    
    int64_t last_fast_read_time = esp_timer_get_time(); // µs since boot
    int64_t last_slow_read_time = last_fast_read_time;

    while(true)
    {
        // Sampling rates:
        // hx711:  1 / 100ms    (blocking)
        // bme280: 1 / 62.5ms   (blocking)
        // ccs811: 1 / 1s       (implementation does not block)

        for(int i=0; i<10; i++)
        {
            vTaskDelay(80 / portTICK_PERIOD_MS);

            // Get time since last read and pass it down the filter-cascade.
            const int64_t fast_read_time = esp_timer_get_time();
            const double fast_read_dt = (double)(fast_read_time - last_fast_read_time) / 1e6;
            last_fast_read_time = fast_read_time;

            fast_sensor_data_t fast_data = {};
            read_fast_data_from_sensors(&fast_data, fast_read_dt);
            ringbuffer_push(sensor_ringbuffer_fast_data, &fast_data);
        }

        {
            const int64_t slow_read_time = esp_timer_get_time();
            const double slow_read_dt = (double)(slow_read_time - last_slow_read_time) / 1e6;
            last_slow_read_time = slow_read_time;
            ESP_LOGI(TAG, "slow_read_dt: %0.1fs", slow_read_dt);
        }

        slow_sensor_data_t slow_data = {};
        read_slow_data_from_sensors(&slow_data);
        ringbuffer_push(sensor_ringbuffer_slow_data, &slow_data);

        // The ccs811 sensor needs to know the external temperature and humidity to perform some corrections.
        ccs811_set_environment_data(slow_data.temperature, slow_data.humidity);
    }
}

static size_t create_fast_sensor_data_line_protocol(char *message_buffer, size_t message_buffer_size)
{
    assert(message_buffer);
    assert(message_buffer_size);
    
    memset(message_buffer, 0, message_buffer_size);

    size_t message_buffer_offset = 0;
    size_t data_count = 0;

    while(true)
    {
        const size_t free_space = message_buffer_size - message_buffer_offset;
        if (free_space < 256) {
            ESP_LOGE(TAG, "http message buffer is full");
            break;
        }

        fast_sensor_data_t data = {};
        bool got_data = ringbuffer_try_pop(sensor_ringbuffer_fast_data, &data);
        if (!got_data) break;

        char item_buffer[256] = {}; // TODO direkt in message buffer ?
        size_t item_buffer_length = snprintf(item_buffer, sizeof(item_buffer),
            "scales,scale_id=CAT1 weight_raw=%0.1f,weight=%0.1f %"PRIu64"\n",
            data.weight_raw, data.weight, data.timestamp);
        
        strcpy(message_buffer + message_buffer_offset, item_buffer);
        message_buffer_offset += item_buffer_length;
        data_count++;
    }

    return data_count;
}

static size_t create_slow_sensor_data_line_protocol(char *message_buffer, size_t message_buffer_size)
{
    assert(message_buffer);
    assert(message_buffer_size);
    
    memset(message_buffer, 0, message_buffer_size);

    size_t message_buffer_offset = 0;
    size_t data_count = 0;

    while(true)
    {
        const size_t free_space = message_buffer_size - message_buffer_offset;
        if (free_space < 256) {
            ESP_LOGE(TAG, "http message buffer is full");
            break;
        }

        slow_sensor_data_t data = {};
        bool got_data = ringbuffer_try_pop(sensor_ringbuffer_slow_data, &data);
        if (!got_data) break;

        char item_buffer[256] = {}; // TODO direkt in message buffer ?
        size_t item_buffer_length = snprintf(item_buffer, sizeof(item_buffer),
            "scales,scale_id=CAT1 temperature=%0.3f,humidity=%0.3f,pressure=%0.3f,co2=%u,tvoc=%u %"PRIu64"\n",
            data.temperature, data.humidity, data.pressure, data.co2, data.tvoc, data.timestamp);
        
        strcpy(message_buffer + message_buffer_offset, item_buffer);
        message_buffer_offset += item_buffer_length;
        data_count++;
    }

    return data_count;
}

static void sensors_post_task(void *task_args)
{
    ESP_LOGI(TAG, "sensors_post_task");

    const size_t message_buffer_size = 16 * 1024;
    char * const message_buffer = malloc(message_buffer_size);
    assert(message_buffer);

    while(true)
    {
        vTaskDelay(10 * 1000 / portTICK_PERIOD_MS); // TODO wenn zuvor puffer voll war, nicht warten und direkt weiter

        const size_t fast_data_count = create_fast_sensor_data_line_protocol(message_buffer, message_buffer_size);
        ESP_LOGI(TAG, "posting %u fast items (%u bytes) ...", fast_data_count, strlen(message_buffer));
        if (fast_data_count) {
            esp_err_t ret = http_post_sensor_data_influx(message_buffer);
            if (ret != ESP_OK) {
                ESP_LOGE(TAG, "failed to post fast sensor data");
            }
        }

        const size_t slow_data_count = create_slow_sensor_data_line_protocol(message_buffer, message_buffer_size);
        ESP_LOGI(TAG, "posting %u slow items (%u bytes) ...", slow_data_count, strlen(message_buffer));
        if (slow_data_count) {
            esp_err_t ret = http_post_sensor_data_influx(message_buffer);
            if (ret != ESP_OK) {
                ESP_LOGE(TAG, "failed to post slow sensor data");
            }
        }
    }
}
