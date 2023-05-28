#pragma once

#include <esp_err.h>

esp_err_t measurement_init(void);

void measurement_mark_start_of_event(void);
void measurement_push_stable_phase(double length, double value);
void measurement_mark_end_of_event(void);

void measurement_update_environment_data(double temperature, double humidity, double pressure);
