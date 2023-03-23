#pragma once

#include <esp_err.h>

esp_err_t measurement_init();

double measurement_process_raw_weight(uint32_t weight_raw);
int measurement_process_corrected_weight(double weight);
