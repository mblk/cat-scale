#pragma once

#include <esp_err.h>

esp_err_t bme280_user_init();

esp_err_t bme280_user_read_from_sensor(double *temp, double *pres, double *hum);

//void bme280_sensor_task();
