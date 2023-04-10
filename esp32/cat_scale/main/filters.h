#pragma once

#include <stddef.h>
#include <stdbool.h>

typedef struct {
    // config
    const size_t window_size;
    // state
    bool reset;
    double * const prev_values;
} mean_filter_t;

mean_filter_t *create_mean_filter(size_t window_size);
void destroy_mean_filter(mean_filter_t *filter);
double mean_filter(mean_filter_t *filter, double input);

typedef struct {
    // config
    const size_t window_size;
    // state
    bool reset;
    double * const prev_values;
} median_filter_t;

median_filter_t *create_median_filter(size_t window_size);
void destroy_median_filter(median_filter_t *filter);
double median_filter(median_filter_t *filter, double input);

typedef struct {
    // settings
    const double alpha;
    // state
    bool reset;
    double prev_output;
} low_pass_filter_t;

low_pass_filter_t *create_low_pass_filter(double sampling_frequency, double cutoff_frequency);
void destroy_low_pass_filter(low_pass_filter_t *filter);
double low_pass_filter(low_pass_filter_t *filter, double input);

typedef struct {
    // settings
    const double alpha;
    // state
    bool reset;
    double prev_input;
    double prev_output;
} high_pass_filter_t;

high_pass_filter_t *create_high_pass_filter(double sampling_frequency, double cutoff_frequency);
void destroy_high_pass_filter(high_pass_filter_t *filter);
double high_pass_filter(high_pass_filter_t *filter, double input);

typedef struct {
    // config
    const double dt;
    // state
    bool reset;
    double prev_input;
} differentiator_t;

differentiator_t *create_differentiator(double sampling_frequency);
void destroy_differentiator(differentiator_t *filter);
double differentiate(differentiator_t *filter, double input);
