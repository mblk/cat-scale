#pragma once

#include <esp_err.h>

esp_err_t ccs811_init();

esp_err_t ccs811_get_latest_values(uint32_t *co2, uint32_t *tvoc);

esp_err_t ccs811_set_environment_data(double temperature, double humidity);
