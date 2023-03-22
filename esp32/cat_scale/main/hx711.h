#pragma once

#include <stdint.h>
#include <esp_err.h>

esp_err_t hx711_init();

uint32_t hx711_read_from_sensor();
