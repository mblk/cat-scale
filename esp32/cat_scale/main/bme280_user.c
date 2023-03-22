#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "bme280_user.h"
#include "bme280.h"

#include "sdkconfig.h"

#include <stdio.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/event_groups.h>

#include <esp_system.h>
#include <esp_event.h>
#include <esp_log.h>

#include <driver/i2c.h>

static const char *TAG = "bme280";

static void bme280_user_delay_ms(uint32_t period_ms, void *intf_ptr);
static int8_t bme280_user_i2c_read(uint8_t reg_addr, uint8_t *reg_data, uint32_t len, void *intf_ptr);
static int8_t bme280_user_i2c_write(uint8_t reg_addr, const uint8_t *reg_data, uint32_t len, void *intf_ptr);

static struct bme280_dev dev = {};

esp_err_t bme280_user_init()
{
    ESP_LOGI(TAG, "bme280_user_init ...");

    dev.intf = BME280_I2C_INTF;
    dev.read = bme280_user_i2c_read;
    dev.write = bme280_user_i2c_write;
    dev.delay_us = bme280_user_delay_ms;

    int8_t rslt = BME280_OK;
    rslt = bme280_init(&dev);
    ESP_ERROR_CHECK((rslt == BME280_OK) ? ESP_OK : ESP_FAIL);

	// Recommended mode of operation: Indoor navigation
	dev.settings.osr_h = BME280_OVERSAMPLING_1X;
	dev.settings.osr_p = BME280_OVERSAMPLING_16X;
	dev.settings.osr_t = BME280_OVERSAMPLING_2X;
	dev.settings.filter = BME280_FILTER_COEFF_16;
	dev.settings.standby_time = BME280_STANDBY_TIME_62_5_MS;

	uint8_t settings_sel;
	settings_sel = BME280_OSR_PRESS_SEL;
	settings_sel |= BME280_OSR_TEMP_SEL;
	settings_sel |= BME280_OSR_HUM_SEL;
	settings_sel |= BME280_STANDBY_SEL;
	settings_sel |= BME280_FILTER_SEL;
	rslt = bme280_set_sensor_settings(settings_sel, &dev);
	rslt = bme280_set_sensor_mode(BME280_NORMAL_MODE, &dev);

	ESP_LOGI(TAG, "Temperature, Pressure, Humidity");
    for(int i=0; i<10; i++)
    {
	 	dev.delay_us(70, dev.intf_ptr);

        struct bme280_data comp_data = {};
	 	rslt = bme280_get_sensor_data(BME280_ALL, &comp_data, &dev);
        ESP_LOGI(TAG, "%d, %0.2f, %0.2f, %0.2f", rslt, comp_data.temperature, comp_data.pressure, comp_data.humidity);
	}

    return ESP_OK;
}

esp_err_t bme280_user_read_from_sensor(double *temperature, double *pressure, double *humidity)
{
    struct bme280_data comp_data = {};

    int8_t rslt = bme280_get_sensor_data(BME280_ALL, &comp_data, &dev);

    if (rslt == 0)
    {
        *temperature = comp_data.temperature;
        *pressure = comp_data.pressure;
        *humidity = comp_data.humidity;

        return ESP_OK;
    }
    else
    {
        ESP_LOGE(TAG, "Error in bme280_user_read_from_sensor err=%d", rslt);

        *temperature = 0.0;
        *pressure = 0.0;
        *humidity = 0.0;

        return ESP_FAIL;
    }
}

static void bme280_user_delay_ms(uint32_t period_ms, void *intf_ptr)
{
    vTaskDelay(period_ms / portTICK_PERIOD_MS);
}

static int8_t bme280_user_i2c_read(uint8_t reg_addr, uint8_t *reg_data, uint32_t len, void *intf_ptr)
{
    const uint8_t dev_addr = BME280_I2C_ADDR_PRIM;
    const int i2c_master_port = 0;
    const int i2c_timeout_ticks = 1000 / portTICK_PERIOD_MS;

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
        ESP_LOGE(TAG, "error in bme280_user_i2c_read #1 err=%d", ret);
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
        ESP_LOGE(TAG, "error in bme280_user_i2c_read #2 err=%d", ret);
        return -1;
    }

    /* Return 0 for Success, non-zero for failure */
    return 0;
}

static int8_t bme280_user_i2c_write(uint8_t reg_addr, const uint8_t *reg_data, uint32_t len, void *intf_ptr)
{
    const uint8_t dev_addr = BME280_I2C_ADDR_PRIM;
    const int i2c_master_port = 0;
    const int i2c_timeout_ticks = 1000 / portTICK_PERIOD_MS;

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
        ESP_LOGE(TAG, "error in bme280_user_i2c_write #1");
        return -1;
    }

    /* Return 0 for Success, non-zero for failure */
    return 0;
}
