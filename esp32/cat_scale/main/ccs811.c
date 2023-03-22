#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "ccs811.h"

#include "sdkconfig.h"

#include <stdio.h>
#include <string.h>
#include <math.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/event_groups.h>

#include <esp_system.h>
#include <esp_event.h>
#include <esp_log.h>
#include <esp_timer.h>

#include <driver/i2c.h>

static const char *TAG = "ccs811";



#define CCS811_ADDR_PRIMARY             0x5A
#define CCS811_ADDR_SECONDARY           0x5B

#define CCS811_REG_STATUS               0x00
#define CCS811_REG_MEAS_MODE            0x01
#define CCS811_REG_ALG_RESULT_DATA      0x02
#define CCS811_REG_RAW_DATA             0x03
#define CCS811_REG_ENV_DATA             0x05
#define CCS811_REG_NTC                  0x06
#define CCS811_REG_THRESHOLDS           0x10
#define CCS811_REG_BASELINE             0x11
#define CCS811_REG_HW_ID                0x20
#define CCS811_REG_HW_VERSION           0x21
#define CCS811_REG_FW_BOOT_VERSION      0x23
#define CCS811_REG_FW_APP_VERSION       0x24
#define CCS811_REG_ERROR_ID             0xE0
#define CCS811_REG_APP_ERASE            0xF1
#define CCS811_REG_APP_DATA             0xF2
#define CCS811_REG_APP_VERIFY           0xF3
#define CCS811_REG_APP_START            0xF4
#define CCS811_REG_SW_RESET             0xFF

#define CCS811_STATUS_ERROR             0x01
#define CCS811_STATUS_DATA_READY        0x08
#define CCS811_STATUS_APP_VALID         0x10
#define CCS811_STATUS_FW_MODE           0x80

const uint8_t ccs811_reset_seq[4] = { 0x11, 0xE5, 0x72, 0x8A };

static esp_err_t ccs811_i2c_read(uint8_t reg_addr, uint8_t *reg_data, uint32_t len);
static esp_err_t ccs811_i2c_write(uint8_t reg_addr, const uint8_t *reg_data, uint32_t len);
static void ccs811_read_from_sensor();

static int64_t last_measure_time = 0;
static uint32_t last_co2 = 0;
static uint32_t last_tvoc = 0;

esp_err_t ccs811_init() // TODO extend error handling? maybe ignore device in main if init fails?
{
    esp_log_level_set(TAG, ESP_LOG_DEBUG);

    ESP_LOGI(TAG, "ccs811_init");

    // check device compatibility.
    uint8_t status = 0;
    uint8_t hw_id = 0;
    uint8_t hw_version = 0;

    ccs811_i2c_read(CCS811_REG_STATUS, &status, 1);
    ccs811_i2c_read(CCS811_REG_HW_ID, &hw_id, 1);
    ccs811_i2c_read(CCS811_REG_HW_VERSION, &hw_version, 1);

    ESP_LOGD(TAG, "status %x hw_id %x hw_version %x", status, hw_id, hw_version);

    if (hw_id != 0x81 || (hw_version & 0xF0) != 0x10)
    {
        ESP_LOGE(TAG, "hw id/version mismatch (%x, %x)", hw_id, hw_version);
        return ESP_FAIL;
    }

    // reset device and enter boot mode.
    ccs811_i2c_write(CCS811_REG_SW_RESET, ccs811_reset_seq, sizeof(ccs811_reset_seq));
    vTaskDelay(50 / portTICK_PERIOD_MS);

    ccs811_i2c_read(CCS811_REG_STATUS, &status, 1);
    ESP_LOGD(TAG, "status after reset %x", status);

    if (status & CCS811_STATUS_FW_MODE) {
        ESP_LOGE(TAG, "device is in application mode after reset");
        return ESP_FAIL;
    }

    if (!(status & CCS811_STATUS_APP_VALID)) {
        ESP_LOGE(TAG, "no application firmware loaded");
        return ESP_FAIL;
    }

    if (status & CCS811_STATUS_DATA_READY) {
        ESP_LOGE(TAG, "data ready flag is set after reset");
        return ESP_FAIL;
    }

    if (status & CCS811_STATUS_ERROR) {
        ESP_LOGE(TAG, "error flag is set after reset");
        return ESP_FAIL;
    }

    // enter application mode.
    ccs811_i2c_write(CCS811_REG_APP_START, NULL, 0);
    vTaskDelay(50 / portTICK_PERIOD_MS);
    
    ccs811_i2c_read(CCS811_REG_STATUS, &status, 1);
    ESP_LOGD(TAG, "status after app_start %x", status);

    if (!(status & CCS811_STATUS_FW_MODE)) {
        ESP_LOGE(TAG, "device is in boot mode after sw_start");
        return ESP_FAIL;
    }
    if (status & CCS811_STATUS_ERROR) {
        ESP_LOGE(TAG, "error flag is set after sw_start");
        return ESP_FAIL;
    }

    // set measurement mode.
    uint8_t mode = 0;
    ccs811_i2c_read(CCS811_REG_MEAS_MODE, &mode, 1);
    ESP_LOGD(TAG, "mode after app_start %x", mode);

    mode = (1 << 4); // 1/s
    ccs811_i2c_write(CCS811_REG_MEAS_MODE, &mode, 1);
    vTaskDelay(50 / portTICK_PERIOD_MS);

    ccs811_i2c_read(CCS811_REG_MEAS_MODE, &mode, 1);
    ESP_LOGD(TAG, "mode after mode set %x", mode);

    // restore baseline.
    //const uint8_t prev_baseline[2] = { 0x1C, 0xBB };
    //const uint8_t prev_baseline[2] = { 0x18, 0xBD };
    // ccs811_i2c_write(CCS811_REG_BASELINE, prev_baseline, sizeof(prev_baseline));
    // vTaskDelay(50 / portTICK_PERIOD_MS);

    // try reading some samples.
    uint8_t baseline1[2] = {};
    ccs811_i2c_read(CCS811_REG_BASELINE, baseline1, sizeof(baseline1));
    ESP_LOGD(TAG, "baseline1 %x %x", baseline1[0], baseline1[1]);

    for(int i=0; i<3; i++)
    {
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        ccs811_read_from_sensor();
    }

    uint8_t baseline2[2] = {};
    ccs811_i2c_read(CCS811_REG_BASELINE, baseline2, sizeof(baseline2));
    ESP_LOGD(TAG, "baseline2 %x %x", baseline2[0], baseline2[1]);

    return ESP_OK;
}

static uint8_t current_baseline[2] = {};

esp_err_t ccs811_get_latest_values(uint32_t *co2, uint32_t *tvoc)
{
    assert(co2);
    assert(tvoc);

    const int64_t t_now = esp_timer_get_time(); // us
    const int64_t t_diff = t_now - last_measure_time;

    esp_err_t ret;

    // has 1 second passed since last (successful) read?
    if (t_diff > 1 * 1000 * 1000)
    {
        // Data ready?
        uint8_t status = 0;
        ret = ccs811_i2c_read(CCS811_REG_STATUS, &status, 1);
        if (ret == ESP_OK && (status & CCS811_STATUS_DATA_READY))
        {
            uint8_t data[4] = {}; // only read 4 bytes (out of 8)
            esp_err_t ret = ccs811_i2c_read(CCS811_REG_ALG_RESULT_DATA, data, sizeof(data));
            if (ret == ESP_OK)
            {
                last_measure_time = t_now;
                last_co2 = (data[0] << 8) | data[1];
                last_tvoc = (data[2] << 8) | data[3];

                // New baseline?
                uint8_t baseline[2] = {};
                if (ccs811_i2c_read(CCS811_REG_BASELINE, baseline, sizeof(baseline)) == ESP_OK)
                {
                    if (memcmp(current_baseline, baseline, 2) != 0)
                    {
                        ESP_LOGD(TAG, "new baseline: %x %x", baseline[0], baseline[1]);
                        memcpy(current_baseline, baseline, 2);
                        // TODO save to nvs & restore on next start
                    }
                }
            }
        }
    }

    *co2 = last_co2;
    *tvoc = last_tvoc;

    return ESP_OK;
}

static uint16_t ccs811_double_to_u16(double input)
{
    int x = round(input);

    if (x < 0)
        x = 0;

    if (x > 0xFFFF)
        x = 0xFFFF;

    return (uint16_t)x;
}

esp_err_t ccs811_set_environment_data(double temperature, double humidity)
{
    //ESP_LOGD(TAG, "set env data %0.2f %0.2f", temperature, humidity);

    const double humidity_1_512_th = humidity / (1. / 512.);
    const double temperature_1_512_th = (temperature + 25.) / (1. / 512.);

    //printf("h %f t %f\n", humidity_1_512_th, temperature_1_512_th);

    const uint16_t h = ccs811_double_to_u16(humidity_1_512_th);
    const uint16_t t = ccs811_double_to_u16(temperature_1_512_th);

    //printf("h %x t %x\n", h, t);

    const uint8_t data[4] =
    {
        (h & 0xFF00) >> 8,
        (h & 0xFF),

        (t & 0xFF00) >> 8,
        (t & 0xFF),
    };

    //ESP_LOG_BUFFER_HEX(TAG, data, sizeof(data));

    return ccs811_i2c_write(CCS811_REG_ENV_DATA, data, sizeof(data));
}

static void ccs811_read_from_sensor()
{
    while(true)
    {
        uint8_t status = 0;
        ccs811_i2c_read(CCS811_REG_STATUS, &status, 1);

        if (status & CCS811_STATUS_DATA_READY)
            break;

        vTaskDelay(1);
    }

    uint8_t data[8] = {};
    ccs811_i2c_read(CCS811_REG_ALG_RESULT_DATA, data, sizeof(data));

    ESP_LOG_BUFFER_HEX(TAG, data, sizeof(data));
}

#if 0
static esp_err_t ccs811_i2c_read__(uint8_t reg_addr, uint8_t *reg_data, uint32_t len)
{
    const uint8_t dev_addr = CCS811_ADDR_PRIMARY;
    const int i2c_master_port = 0;
    const int i2c_timeout_ticks = 1000 / portTICK_PERIOD_MS;

    // xxx
    //esp_rom_delay_us(1000);
    // xxx

    esp_err_t ret;

    i2c_cmd_handle_t cmd = i2c_cmd_link_create();
    i2c_master_start(cmd);
    {
        i2c_master_write_byte(cmd, (dev_addr << 1) | I2C_MASTER_WRITE, true);
        i2c_master_write_byte(cmd, reg_addr, true);
    }
    i2c_master_stop(cmd);
    ret = i2c_master_cmd_begin(i2c_master_port, cmd, i2c_timeout_ticks);
    i2c_cmd_link_delete(cmd);

    if (ret != ESP_OK) {
        ESP_LOGE(TAG, "error in ccs811_i2c_read reg=%x err=%x %s", reg_addr, ret, esp_err_to_name(ret));
        return -1;
    }

    cmd = i2c_cmd_link_create();
    i2c_master_start(cmd);
    {
        i2c_master_write_byte(cmd, (dev_addr << 1) | I2C_MASTER_READ, true);

        for(uint32_t i=0; i<len; i++)
            i2c_master_read_byte(cmd, reg_data + i, (i == len-1) ? I2C_MASTER_LAST_NACK : I2C_MASTER_ACK);
    }
    i2c_master_stop(cmd);
    ret = i2c_master_cmd_begin(i2c_master_port, cmd, i2c_timeout_ticks);
    i2c_cmd_link_delete(cmd);

    if (ret != ESP_OK)
    {
        ESP_LOGE(TAG, "error in ccs811_i2c_read reg=%x err=%x %s", reg_addr, ret, esp_err_to_name(ret));
        return ret;
    }

    return ESP_OK;
}
#endif

static esp_err_t ccs811_i2c_read(uint8_t reg_addr, uint8_t *reg_data, uint32_t len)
{
    const uint8_t dev_addr = CCS811_ADDR_PRIMARY;
    const int i2c_master_port = 0;
    const int i2c_timeout_ticks = 1000 / portTICK_PERIOD_MS;

    // xxx
    //esp_rom_delay_us(1000);
    // xxx

    esp_err_t ret;

    i2c_cmd_handle_t cmd = i2c_cmd_link_create();
    i2c_master_start(cmd);
    {
        i2c_master_write_byte(cmd, (dev_addr << 1) | I2C_MASTER_WRITE, true);
        i2c_master_write_byte(cmd, reg_addr, true);
    }
    i2c_master_start(cmd);
    {
        i2c_master_write_byte(cmd, (dev_addr << 1) | I2C_MASTER_READ, true);

        for(uint32_t i=0; i<len; i++)
            i2c_master_read_byte(cmd, reg_data + i, (i == len-1) ? I2C_MASTER_LAST_NACK : I2C_MASTER_ACK);
    }
    i2c_master_stop(cmd);
    ret = i2c_master_cmd_begin(i2c_master_port, cmd, i2c_timeout_ticks);
    i2c_cmd_link_delete(cmd);

    if (ret != ESP_OK)
    {
        ESP_LOGE(TAG, "error in ccs811_i2c_read reg=%x err=%x %s", reg_addr, ret, esp_err_to_name(ret));
        return ret;
    }

    return ESP_OK;
}

static esp_err_t ccs811_i2c_write(uint8_t reg_addr, const uint8_t *reg_data, uint32_t len)
{
    const uint8_t dev_addr = CCS811_ADDR_PRIMARY;
    const int i2c_master_port = 0;
    const int i2c_timeout_ticks = 1000 / portTICK_PERIOD_MS;

    // xxx
    //esp_rom_delay_us(1000);
    // xxx

    esp_err_t ret;
    i2c_cmd_handle_t cmd = i2c_cmd_link_create();

    i2c_master_start(cmd);
    {
        i2c_master_write_byte(cmd, (dev_addr << 1) | I2C_MASTER_WRITE, true);
        i2c_master_write_byte(cmd, reg_addr, true);

        for(uint32_t i=0; i<len; i++)
            i2c_master_write_byte(cmd, reg_data[i], true);
    }
    i2c_master_stop(cmd);

    ret = i2c_master_cmd_begin(i2c_master_port, cmd, i2c_timeout_ticks);
    i2c_cmd_link_delete(cmd);

    if (ret != ESP_OK) {
        ESP_LOGE(TAG, "error in ccs811_i2c_write err=%x (%s)", ret, esp_err_to_name(ret));
        return ret;
    }

    return ESP_OK;
}
