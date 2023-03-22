#pragma once

#include <stddef.h>
#include <sys/time.h>

#include <esp_err.h>

esp_err_t time_init_and_sync();

void convert_timeval_to_iso8601(struct timeval tv, char *output, size_t output_size);
