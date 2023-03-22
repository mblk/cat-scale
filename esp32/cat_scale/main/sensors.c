
#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "sensors.h"
#include "hx711.h"
#include "bme280_user.h"
#include "ccs811.h"
#include "time.h"
#include "http.h"

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




typedef struct sensor_data {
    struct timeval time;
    uint32_t weight_raw;
    double weight;
    double temperature;
    double pressure;
    double humidity;
    uint32_t co2;
    uint32_t tvoc;
} sensor_data_t;

typedef struct {
    double setup_time;
    double poo_time;
    double cleanup_time;
    double cat_weight;
    double poo_weight;
} measurement_t;



static const char *TAG = "sensors";

static MessageBufferHandle_t measurement_message_buffer = NULL;

static esp_err_t i2c_master_init(void)
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

static void sensors_read_task();
static void sensors_post_task();

esp_err_t sensors_init()
{
    esp_log_level_set(TAG, ESP_LOG_DEBUG);
    ESP_LOGI(TAG, "sensors_init");

    measurement_message_buffer = xMessageBufferCreate(1024);
    assert(measurement_message_buffer);

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

#define SENSOR_AVERAGE_COUNT    5       // 10Hz / 5 = 2Hz
#define SENSOR_RINGBUFFER_SIZE  128     // Number of items. 128/2Hz -> 64s

static portMUX_TYPE sensor_ringbuffer_spinlock = portMUX_INITIALIZER_UNLOCKED;
static sensor_data_t sensor_ringbuffer[SENSOR_RINGBUFFER_SIZE] = {0};
static size_t sensor_ringbuffer_write_index = 0;
static size_t sensor_ringbuffer_read_index = 0;

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

#if 0
static void dump_sensor_data(const sensor_data_t *sensor_data)
{
    assert(sensor_data);

    char time_buffer[32] = {};
    convert_timeval_to_iso8601(sensor_data->time, time_buffer, sizeof(time_buffer));

    printf(">> time='%s' weight=%u temp=%0.2f pres=%0.2f hum=%0.2f co2=%u tvoc=%u\n", time_buffer, sensor_data->weight,
           sensor_data->temperature, sensor_data->pressure, sensor_data->humidity, sensor_data->co2, sensor_data->tvoc);
}
#endif

static double process_raw_weight(uint32_t weight_raw);
static void process_corrected_weight(double weight);

static void sensors_read_task()
{
    ESP_LOGI(TAG, "sensors_read_task");
    
    while(true)
    {
        sensor_data_t sensor_data[SENSOR_AVERAGE_COUNT] = {};
        sensor_data_t sensor_sum = {};

        // Sampling rates:
        // hx711:  1 / 100ms    (blocking)
        // bme280: 1 / 62.5ms   (blocking)
        // ccs811: 1 / 1s       (implementation does not block)

        for (size_t i=0; i<SENSOR_AVERAGE_COUNT; i++)
        {
            vTaskDelay(80 / portTICK_PERIOD_MS); // target is 100ms

            read_data_from_sensors(sensor_data + i);
            //dump_sensor_data(sensor_data + i);

            sensor_sum.weight_raw += sensor_data->weight_raw;
            sensor_sum.temperature += sensor_data->temperature;
            sensor_sum.pressure += sensor_data->pressure;
            sensor_sum.humidity += sensor_data->humidity;
        }

        // sensor_sum.tv.tv_sec = (sensor_data[0].tv.tv_sec + sensor_data[SENSOR_AVERAGE_COUNT-1].tv.tv_sec) / 2;
        // sensor_sum.tv.tv_usec = (sensor_data[0].tv.tv_usec + sensor_data[SENSOR_AVERAGE_COUNT-1].tv.tv_usec) / 2;

        sensor_sum.time = sensor_data[0].time; // TODO

        sensor_sum.weight_raw /= SENSOR_AVERAGE_COUNT;
        sensor_sum.temperature /= SENSOR_AVERAGE_COUNT;
        sensor_sum.pressure /= SENSOR_AVERAGE_COUNT;
        sensor_sum.humidity /= SENSOR_AVERAGE_COUNT;

        sensor_sum.co2 = sensor_data[0].co2; // TODO
        sensor_sum.tvoc = sensor_data[0].tvoc; // TODO

        //dump_sensor_data(&sensor_sum);

        ccs811_set_environment_data(sensor_sum.temperature, sensor_sum.humidity);

        sensor_sum.weight = process_raw_weight(sensor_sum.weight_raw);
        process_corrected_weight(sensor_sum.weight);

        sensor_ringbuffer_push(&sensor_sum);

        // TODO freq messen und ausgeben?
        //esp_timer_get_time() // us
    }
}

#if 0
static size_t create_sensor_data_json(char *message_buffer, size_t message_buffer_size)
{
    assert(message_buffer);
    assert(message_buffer_size);
    
    memset(message_buffer, 0, message_buffer_size);

    if (message_buffer_size < 10)
        return 0;

    size_t message_buffer_offset = 0;
    size_t data_count = 0;

    strcat(message_buffer, "[\n");
    message_buffer_offset = strlen(message_buffer);

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

        char time_buffer[32] = {};
        convert_timeval_to_iso8601(data.time, time_buffer, sizeof(time_buffer));

        char item_buffer[256] = {};
        size_t item_buffer_length = snprintf(item_buffer, sizeof(item_buffer),
            "{"
            "\"sensorId\":\"CAT_SCALE_1\","
            "\"timeStamp\":\"%s\","
            "\"weight\":%u,"
            "\"temperature\":%0.3f,"
            "\"pressure\":%0.3f,"
            "\"humidity\":%0.3f,"
            "\"co2\":%u,"
            "\"tvoc\":%u"
            "},\n",
            time_buffer, data.weight, data.temperature, data.pressure, data.humidity, data.co2, data.tvoc);
        
        strcpy(message_buffer + message_buffer_offset, item_buffer);
        message_buffer_offset += item_buffer_length;
        data_count++;
    }

    if (message_buffer_offset >= 2 && message_buffer[message_buffer_offset-2] == ',')
    {
        message_buffer[message_buffer_offset-2] = 0;
        message_buffer_offset -= 2;
    }

    strcpy(message_buffer + message_buffer_offset, "\n]");

    // printf("got items: %u\n", data_count);
    // printf("buffer_offset: %u\n", message_buffer_offset);
    //printf("message buffer:\nSTART\n%s\nEND\n", message_buffer);
    //printf("message_buffer: %u / %u (%u free)\n", strlen(message_buffer), message_buffer_size, message_buffer_size - strlen(message_buffer));

    return data_count;
}
#endif

static size_t create_sensor_data_line_protocol(char *message_buffer, size_t message_buffer_size)
{
    assert(message_buffer);
    assert(message_buffer_size);
    
    memset(message_buffer, 0, message_buffer_size);

    if (message_buffer_size < 10)
        return 0;

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
            "scales,scale_id=CAT1 weight_raw=%u,weight=%0.3f,temperature=%0.3f,humidity=%0.3f,pressure=%0.3f,co2=%u,tvoc=%u %"PRIu64"\n",
            data.weight_raw, data.weight, data.temperature, data.humidity, data.pressure, data.co2, data.tvoc, timestamp);
        
        strcpy(message_buffer + message_buffer_offset, item_buffer);
        message_buffer_offset += item_buffer_length;
        data_count++;
    }

    //printf("got items: %u\n", data_count);
    //printf("buffer_offset: %u\n", message_buffer_offset);
    //printf("message buffer:\nSTART\n%s\nEND\n", message_buffer);
    //printf("message_buffer: %u / %u (%u free)\n", strlen(message_buffer), message_buffer_size, message_buffer_size - strlen(message_buffer));

    return data_count;
}

static size_t create_measurement_json(char *message_buffer, size_t message_buffer_size, const measurement_t *measurement)
{
    struct timeval tv;
    gettimeofday(&tv, NULL);

    char time_buffer[32] = {};
    convert_timeval_to_iso8601(tv, time_buffer, sizeof(time_buffer));

    return snprintf(message_buffer, message_buffer_size,
        "{"
        "\"timeStamp\":\"%s\","
        "\"setupTime\":%0.3f,"
        "\"pooTime\":%0.3f,"
        "\"cleanupTime\":%0.3f,"
        "\"catWeight\":%0.3f,"
        "\"pooWeight\":%0.3f"
        "}",
        time_buffer,
        measurement->setup_time, measurement->poo_time, measurement->cleanup_time,
        measurement->cat_weight, measurement->poo_weight);
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

        measurement_t m = {};
        size_t bytes_read = xMessageBufferReceive(measurement_message_buffer, &m, sizeof(measurement_t), 0);
        if (bytes_read)
        {
            if (bytes_read == sizeof(measurement_t))
            {
                create_measurement_json(message_buffer, message_buffer_size, &m);
                http_post_json_data(message_buffer);
            }
            else
            {
                ESP_LOGE(TAG, "Incomplete read from measurement buffer (got %u/%u)", bytes_read, sizeof(measurement_t));
            }
        }
    }
}




//
// TODO move business logic ?
//

static void calculate_spread_avg_uint32(const uint32_t *values, size_t count, uint32_t *spread, uint32_t *avg)
{
    assert(values);
    assert(count);
    assert(spread);
    assert(avg);

    uint32_t min = values[0];
    uint32_t max = values[0];
    uint64_t sum = 0;

    for(size_t i=0; i<count; i++)
    {
        uint32_t v = values[i];
        sum += v;
        if (v < min) min = v;
        if (v > max) max = v;
    }

    *spread = max - min;
    *avg = (uint32_t)round((double)sum / (double)count);
}

static void calculate_spread_avg_double(const double *values, size_t count, double *spread, double *avg)
{
    assert(values);
    assert(count);
    assert(spread);
    assert(avg);

    double min = values[0];
    double max = values[0];
    double sum = 0;

    for(size_t i=0; i<count; i++)
    {
        double v = values[i];
        sum += v;
        if (v < min) min = v;
        if (v > max) max = v;
    }

    *spread = max - min;
    *avg = sum / (double)count;
}



#define ZERO_DETECTION_TICKS (2*60) // 60s

static uint32_t raw_weight_prev_values[ZERO_DETECTION_TICKS] = {};
static size_t raw_weight_prev_value_index = 0;
static uint32_t raw_weight_zero_offset = 0;

static const uint32_t zeroing_threshold = 25 * 100; // ~100g



static double process_raw_weight(uint32_t weight_raw) // called at 2 Hz
{
    //ESP_LOGI(TAG, "raw %u", weight_raw);

    // first time?
    if (raw_weight_zero_offset == 0)
        raw_weight_zero_offset = weight_raw;

    // store values in ring-buffer
    raw_weight_prev_values[raw_weight_prev_value_index] = weight_raw;
    raw_weight_prev_value_index = (raw_weight_prev_value_index + 1) % ZERO_DETECTION_TICKS;

    // inspect signal
    uint32_t spread=0, avg=0;
    calculate_spread_avg_uint32(raw_weight_prev_values, ZERO_DETECTION_TICKS, &spread, &avg);
    //ESP_LOGI(TAG, "raw spread over %u samples was %u", ZERO_DETECTION_TICKS, spread);

    if (spread < zeroing_threshold)
    {
        raw_weight_zero_offset = avg;
        //ESP_LOGI(TAG, "new zero offset %u", raw_weight_zero_offset);
    }

    const int32_t weight_raw_zeroed = (int32_t)weight_raw - (int32_t)raw_weight_zero_offset;


    // 8.612.000
    // 8.825.500
    // 9.8kg

    // convert raw value to grams
    const uint32_t v_zero  = 8612000;
    const uint32_t v_calib = 8825500;
    const uint32_t m_zero  = 0;
    const uint32_t m_calib = 9800; // g
    const uint32_t dv = v_calib - v_zero;
    const uint32_t dm = m_calib - m_zero;
    const double v_per_m = (double)dv / (double)dm; // change in value per gram
    // const uint32_t v_zero  = 8339000;
    // const uint32_t v_calib = 8365000;
    // const uint32_t m_zero  = 0;
    // const uint32_t m_calib = 1100; // g
    // const uint32_t dv = v_calib - v_zero;
    // const uint32_t dm = m_calib - m_zero;
    // const double v_per_m = (double)dv / (double)dm; // change in value per gram

    const double weight = (double)weight_raw_zeroed / v_per_m;

    return weight;
}



#define WEIGHT_HISTORY_SAMPLES  (2*5)   // 5s

#define WEIGHT_SAMPLE_DT        0.5     // 2Hz, 0.5s

static double weight_history_values[WEIGHT_HISTORY_SAMPLES] = {};
static size_t weight_history_index = 0;

static int measure_state = 0;
static int time_in_current_state = 0;



static measurement_t current_measurement = {};




static void push_measurement()
{
    ESP_LOGI(TAG, "MMM setup=%0.1f poo=%0.1f cleanup=%0.1f cat_weight=%0.1f poo_weight=%0.1f",
        current_measurement.setup_time, current_measurement.poo_time, current_measurement.cleanup_time,
        current_measurement.cat_weight, current_measurement.poo_weight);

    size_t bytes_written = xMessageBufferSend(measurement_message_buffer, &current_measurement, sizeof(measurement_t), 0);

    if (bytes_written != sizeof(measurement_t)) {
        ESP_LOGE(TAG, "Failed to add measurement to buffer");
    }
}

static void enter_state(int new_state)
{
    measure_state = new_state;
    time_in_current_state = 0;
}

static void process_corrected_weight(double weight) // called at 2 Hz
{
    // store values in ring-buffer
    weight_history_values[weight_history_index] = weight;
    weight_history_index = (weight_history_index + 1) % WEIGHT_HISTORY_SAMPLES;

    // inspect signal
    double spread=0, avg=0;
    calculate_spread_avg_double(weight_history_values, WEIGHT_HISTORY_SAMPLES, &spread, &avg);

    const bool is_stable = spread < 25.0; // TODO dynamic based on recent noise levels?
    const bool is_zero = avg < 10.0;
    const bool is_plausible_cat = 1000.0 < avg && avg < 10000.0;

    ESP_LOGI(TAG, "state=%d, weight=%0.1f, is_stable=%s, spread %0.1f, avg %0.1f", measure_state, weight, is_stable ? "yes" : "no", spread, avg);

    // timeout?
    time_in_current_state++;
    if (measure_state > 1 && time_in_current_state > 60*2)
    {
        ESP_LOGE(TAG, "Timeout in state %d", measure_state);
        enter_state(0);
    }

    // TODO cleaning event = negative weight for > x samples ?

    switch (measure_state)
    {
        case 0: // wait for zero
        {
            // reset
            memset(&current_measurement, 0, sizeof(measurement_t));

            if (is_stable && is_zero)
            {
                enter_state(1);
            }
            break;
        }

        case 1: // wait for significant weight
        {
            if (is_plausible_cat)
            {
                ESP_LOGI(TAG, "Start of measurement");
                enter_state(2);
            }
            break;
        }

        case 2: // wait for stable signal
        {
            current_measurement.setup_time += WEIGHT_SAMPLE_DT;

            if (is_stable && is_plausible_cat)
            {
                ESP_LOGI(TAG, "Poo started 5s ago. Measurement: %0.1f", avg);
                current_measurement.setup_time -= WEIGHT_HISTORY_SAMPLES * WEIGHT_SAMPLE_DT;
                current_measurement.poo_time += WEIGHT_HISTORY_SAMPLES * WEIGHT_SAMPLE_DT;
                current_measurement.cat_weight = avg;
                enter_state(3);
            }
            break;
        }

        case 3: // wait for end of stable signal
        {
            current_measurement.poo_time += WEIGHT_SAMPLE_DT;

            if (!is_stable)
            {
                ESP_LOGI(TAG, "End of poo");
                enter_state(4);
            }
            break;
        }

        case 4: // wait for cat to leave
        {
            current_measurement.cleanup_time += WEIGHT_SAMPLE_DT;

            if (is_stable && !is_plausible_cat)
            {
                current_measurement.poo_weight = avg;
                ESP_LOGI(TAG, "End of measurement");
                push_measurement();
                enter_state(0);
            }
            break;
        }

        default:
        {
            ESP_LOGE(TAG, "invalid state %d", measure_state);
            enter_state(0);
            break;
        }
    }
}
