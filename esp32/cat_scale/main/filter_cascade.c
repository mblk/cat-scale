#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "filter_cascade.h"
#include "filters.h"
#include "measurement.h"

#include <string.h>
#include <math.h>
#include <stdbool.h>
#include <assert.h>

static high_pass_filter_t *g_hpf = NULL;
static low_pass_filter_t *g_lpf = NULL;
static mean_filter_t *g_mean = NULL;
static median_filter_t *g_median = NULL;
static differentiator_t *g_dxdt = NULL;

#if DEBUG_FILTER_CASCADE
static double g_first_input_value = 0.0;
#endif

#define HPF_HISTORY_SIZE    (10)
#define STABLE_VALUES_SIZE  (1000)

static bool g_input_switch = false;
static double g_input_offset = 0.0;
static double g_prev_hpf_offsets[HPF_HISTORY_SIZE] = {};
static double g_input_switch_timer = 0.0;

static double g_stable_time = 0.0;
static double g_stable_phase_values[STABLE_VALUES_SIZE] = {};
static int g_stable_phase_values_count = 0;

static const double cfg_sampling_frequency = 10.0;
static const double cfg_dxdt_threshold = 50.0;
static const double cfg_stable_phase_min_time = 2.5;
static const double cfg_hold_timer = 10.0;
static const double cfg_hold_timeout = 300.0;
static const double cfg_hold_weight_low = -500.0;
static const double cfg_hold_weight_high = 5000.0;
static const double cfg_calibration_factor = 1.0 / 23.0;

void filter_cascade_init(void)
{
    g_hpf = create_high_pass_filter(cfg_sampling_frequency, 0.1);
    g_lpf = create_low_pass_filter(cfg_sampling_frequency, 0.5);
    g_mean = create_mean_filter(10);
    g_median = create_median_filter(10);
    g_dxdt = create_differentiator(cfg_sampling_frequency);

#if DEBUG_FILTER_CASCADE
    g_first_input_value = 0.0;
#endif

    g_input_switch = false;
    g_input_offset = 0.0;
    memset(g_prev_hpf_offsets, 0, sizeof(g_prev_hpf_offsets));
    g_input_switch_timer = 0.0;

    g_stable_time = 0.0;
    memset(g_stable_phase_values, 0, sizeof(g_stable_phase_values));
    g_stable_phase_values_count = 0;
}

void filter_cascade_cleanup(void)
{
    if (g_hpf) destroy_high_pass_filter(g_hpf);
    if (g_lpf) destroy_low_pass_filter(g_lpf);
    if (g_mean) destroy_mean_filter(g_mean);
    if (g_median) destroy_median_filter(g_median);
    if (g_dxdt) destroy_differentiator(g_dxdt);

    g_hpf = NULL;
    g_lpf = NULL;
    g_mean = NULL;
    g_median = NULL;
    g_dxdt = NULL;
}

static void push_stable_value(double value, double dt)
{
    g_stable_time += dt;

    if (g_stable_phase_values_count < STABLE_VALUES_SIZE)
        g_stable_phase_values[g_stable_phase_values_count++] = value;
}

static void clear_stable_phase(void)
{
    if (g_stable_time >= cfg_stable_phase_min_time &&
        g_stable_phase_values_count > 0 &&
        g_input_switch)
    {
        double avg = 0;
        for(size_t i=0; i<g_stable_phase_values_count; i++)
            avg += g_stable_phase_values[i];
        avg /= (double)g_stable_phase_values_count;

        measurement_push_stable_phase(g_stable_time, avg);
    }

    g_stable_time = 0.0;
    memset(g_stable_phase_values, 0, sizeof(g_stable_phase_values));
    g_stable_phase_values_count = 0;
}

double filter_cascade_process(double input, double dt)
{
    assert(g_hpf);

#if DEBUG_FILTER_CASCADE
    if (g_first_input_value == 0.0)
        g_first_input_value = input;
#endif

    const double output_hpf1 = high_pass_filter(g_hpf, input);
    const double input_for_lpf = g_input_switch ? (input + g_input_offset) : output_hpf1;
    const double output_lpf = low_pass_filter(g_lpf, input_for_lpf);
    const double output_mean = mean_filter(g_mean, output_lpf);
    const double output_median = median_filter(g_median, output_mean);
    const double output_grams = output_median * cfg_calibration_factor;
    const double output_dxdt = differentiate(g_dxdt, output_grams);

    for(size_t i=HPF_HISTORY_SIZE-1; i>0; i--) g_prev_hpf_offsets[i] = g_prev_hpf_offsets[i-1];
    g_prev_hpf_offsets[0] = output_hpf1 - input;

#if DEBUG_FILTER_CASCADE
    filter_cascade_debug("input", input - g_first_input_value + 50000);
    filter_cascade_debug("hpf hold", g_input_switch ? (100000) : (75000));
    //filter_cascade_debug("hpf out", output_hpf);
    //filter_cascade_debug("lpf in", input_for_lpf);
    //filter_cascade_debug("lpf out", output_lpf);
    //filter_cascade_debug("mean out", output_mean);
    filter_cascade_debug("median out", output_median);
    //filter_cascade_debug("grams out", output_grams);
    //filter_cascade_debug("dxdt out", output_dxdt);
#endif

    const bool signal_stable = fabs(output_dxdt) < cfg_dxdt_threshold;

    const bool hold_trigger = !signal_stable ||
        output_grams < cfg_hold_weight_low ||
        output_grams > cfg_hold_weight_high;

    if (signal_stable)
    {
        push_stable_value(output_grams, dt);
    }
    else
    {
        clear_stable_phase();
    }

    if (hold_trigger)
    {
        // activate switch?
        if (!g_input_switch)
        {
            measurement_mark_start_of_event();
            g_input_switch = true;
            g_input_offset = g_prev_hpf_offsets[HPF_HISTORY_SIZE-1];
        }

        g_input_switch_timer = cfg_hold_timer;
    }

    if (g_input_switch)
    {
        g_input_switch_timer -= dt;

        // deactivate switch?
        if (g_input_switch_timer <= 0 || g_stable_time >= cfg_hold_timeout)
        {
            clear_stable_phase();
            measurement_mark_end_of_event();

            g_input_switch = false;

            g_hpf->reset = true;
            g_lpf->reset = true;
            g_mean->reset = true;
            g_median->reset = true;
            g_dxdt->reset = true;
        }
    }

    return output_grams;
}

