#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "filters.h"

#ifndef UNIT_TESTING
#include "sdkconfig.h"
#endif

#include <stdio.h>
#include <string.h>
#include <math.h>
#include <stdbool.h>

mean_filter_t *create_mean_filter(size_t window_size)
{
    assert(window_size > 1);

    const mean_filter_t filter_config = {
        .window_size = window_size,
        .reset = true,
        .prev_values = malloc(window_size * sizeof(double)),
    };
    assert(filter_config.prev_values);
    memset(filter_config.prev_values, 0, window_size * sizeof(double));

    mean_filter_t * const filter = malloc(sizeof(mean_filter_t));
    assert(filter);
    memcpy(filter, &filter_config, sizeof(mean_filter_t));

    return filter;
}

void destroy_mean_filter(mean_filter_t *filter)
{
    assert(filter);
    free(filter->prev_values);
    free(filter);
}

double mean_filter(mean_filter_t *filter, double input)
{
    assert(filter);

    const size_t window_size = filter->window_size;
    double * const prev_values = filter->prev_values;

    if (filter->reset)
    {
        filter->reset = false;
        for(size_t i=0; i<window_size; i++)
            prev_values[i] = input;
    }

    for(size_t i=window_size-1; i > 0; i--)
        prev_values[i] = prev_values[i-1];
    prev_values[0] = input;

    double sum = 0;
    for(size_t i=0; i<window_size; i++)
        sum += prev_values[i];

    double avg = sum / (double)window_size;
    return avg;
}

median_filter_t *create_median_filter(size_t window_size)
{
    assert(window_size > 1);

    const median_filter_t filter_config = {
        .window_size = window_size,
        .reset = true,
        .prev_values = malloc(window_size * sizeof(double)),
    };
    assert(filter_config.prev_values);
    memset(filter_config.prev_values, 0, window_size * sizeof(double));

    median_filter_t * const filter = malloc(sizeof(median_filter_t));
    assert(filter);
    memcpy(filter, &filter_config, sizeof(median_filter_t));

    return filter;
}

void destroy_median_filter(median_filter_t *filter)
{
    assert(filter);
    free(filter->prev_values);
    free(filter);
}

static int median_filter_sort_func(const void* a, const void* b)
{
   return (*(double*)a > *(double*)b) ? 1 : -1;
}

double median_filter(median_filter_t *filter, double input)
{
    assert(filter);

    const size_t window_size = filter->window_size;
    double * const prev_values = filter->prev_values;

    if (filter->reset)
    {
        filter->reset = false;
        for(size_t i=0; i<window_size; i++)
            prev_values[i] = input;
    }

    for(size_t i=window_size-1; i > 0; i--)
        prev_values[i] = prev_values[i-1];
    prev_values[0] = input;

    double window[window_size];
    memcpy(window, prev_values, window_size * sizeof(double));
    qsort(window, window_size, sizeof(double), median_filter_sort_func);

    double output = window[(window_size-1)/2];
    return output;
}

low_pass_filter_t *create_low_pass_filter(double sampling_frequency, double cutoff_frequency)
{
    const double RC = 1.0 / (cutoff_frequency * 2.0 * 3.141592654);
    const double alpha = 1.0 / (1.0 + RC * sampling_frequency);

    const low_pass_filter_t filter_settings = {
        .alpha = alpha,
        .reset = true,
        .prev_output = 0.0,
    };

    low_pass_filter_t * const filter = malloc(sizeof(low_pass_filter_t));
    assert(filter);
    memcpy(filter, &filter_settings, sizeof(low_pass_filter_t));

    return filter;
}

void destroy_low_pass_filter(low_pass_filter_t *filter)
{
    assert(filter);
    free(filter);
}

double low_pass_filter(low_pass_filter_t *filter, double input)
{
    assert(filter);

    if (filter->reset) {
        filter->reset = false;
        filter->prev_output = input;
    }

    double output = filter->alpha * input + (1.0 - filter->alpha) * filter->prev_output;

    filter->prev_output = output;

    return output;
}

high_pass_filter_t *create_high_pass_filter(double sampling_frequency, double cutoff_frequency)
{
    const double dt = 1.0 / sampling_frequency;
    const double RC = 1.0 / (2.0 * 3.141592654 * cutoff_frequency);
    const double alpha = RC / (RC + dt);

    const high_pass_filter_t filter_settings = {
        .alpha = alpha,
        .reset = true,
        .prev_input = 0.0,
        .prev_output = 0.0,
    };

    high_pass_filter_t * const filter = malloc(sizeof(high_pass_filter_t));
    assert(filter);
    memcpy(filter, &filter_settings, sizeof(high_pass_filter_t));

    return filter;
}

void destroy_high_pass_filter(high_pass_filter_t *filter)
{
    assert(filter);
    free(filter);
}

double high_pass_filter(high_pass_filter_t *filter, double input)
{
    assert(filter);

    if (filter->reset) {
        filter->reset = false;
        filter->prev_input = input;
        filter->prev_output = 0.0;
    }

    double output = filter->alpha * (filter->prev_output + input - filter->prev_input);

    filter->prev_input = input;
    filter->prev_output = output;

    return output;
}

differentiator_t *create_differentiator(double sampling_frequency)
{
    const double dt = 1.0 / sampling_frequency;

    const differentiator_t filter_config = {
        .dt = dt,
        .reset = true,
        .prev_input = 0.0,
    };

    differentiator_t *filter = malloc(sizeof(differentiator_t));
    assert(filter);
    memcpy(filter, &filter_config, sizeof(differentiator_t));

    return filter;
}

void destroy_differentiator(differentiator_t *filter)
{
    assert(filter);
    free(filter);
}

double differentiate(differentiator_t *filter, double input)
{
    assert(filter);

    if (filter->reset) {
        filter->reset = false;
        filter->prev_input = input;
    }

    double output = (input - filter->prev_input) / filter->dt;

    filter->prev_input = input;

    return output;
}
