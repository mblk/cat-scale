#pragma once

#include <esp_err.h>

esp_err_t http_post_sensor_data_influx(const char *sensor_data);
esp_err_t http_post_json_data(const char *path, const char *json);
