#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "filter_cascade.h"
#include "filters.h"
#include "measurement.h"

#include "sdkconfig.h"

#include <stdio.h>
#include <string.h>
#include <math.h>
#include <stdbool.h>
#include <assert.h>

static high_pass_filter_t *g_hpf1 = NULL;
static low_pass_filter_t *g_lpf1 = NULL;
static mean_filter_t *g_mean1 = NULL;
static median_filter_t *g_median1 = NULL;
static differentiator_t *g_dxdt1 = NULL;

static bool g_input_switch = false;
static double g_input_offset = 0;
static double g_prev_hpf1_offsets[10] = {};
static double g_input_switch_timer = 0;

static double g_stable_time = 0.0;
static double g_stable_phase_values[1000] = {}; // TODO
static int g_stable_phase_values_index = 0;

static const double sampling_frequency = 10.0;
static const double dxdt_threshold = 50.0;

void filter_cascade_init(void)
{
    g_hpf1 = create_high_pass_filter(sampling_frequency, 0.1);
    g_lpf1 = create_low_pass_filter(sampling_frequency, 0.5);
    g_mean1 = create_mean_filter(10);
    g_median1 = create_median_filter(10);
    g_dxdt1 = create_differentiator(sampling_frequency);
}

static void push_stable_value(double value, double dt)
{
    g_stable_time += dt;
    g_stable_phase_values[g_stable_phase_values_index % 1000] = value;
    g_stable_phase_values_index++;
}

static void clear_stable_phase(void)
{
    if (g_stable_time >= 2.0)
    {
        int num_values = g_stable_phase_values_index;
        if (g_stable_phase_values_index >= 1000) num_values = 1000;

        double avg = 0;
        for(int i=0; i<num_values; i++)
            avg += g_stable_phase_values[i];
        avg /= (double)num_values;

        //printf("[%u] End of stable phase (%0.1f s, %0.1f avg, %d values)\n", process_value_index, g_stable_time, avg, num_values);
        measurement_push_stable_phase(g_stable_time, avg);
    }

    g_stable_time = 0.0;
    memset(g_stable_phase_values, 0, sizeof(double) * 1000);
    g_stable_phase_values_index = 0;
}

double filter_cascade_process(double input, double dt)
{
    assert(g_hpf1);

    double output_hpf1 = high_pass_filter(g_hpf1, input);

    for(int i=9; i>0; i--) g_prev_hpf1_offsets[i] = g_prev_hpf1_offsets[i-1];
    g_prev_hpf1_offsets[0] = output_hpf1 - input;

    double input_for_lp1 = g_input_switch ? (input + g_input_offset) : output_hpf1;
    double output_lpf1 = low_pass_filter(g_lpf1, input_for_lp1);
    double output_mean1 = mean_filter(g_mean1, output_lpf1);
    double output_median1 = median_filter(g_median1, output_mean1);

    double output_grams = output_median1 / 23.0; // TODO calibration

    double output_dxdt1 = differentiate(g_dxdt1, output_grams);


    if (fabs(output_dxdt1) < dxdt_threshold) // TODO use stddev instead?
    {
        push_stable_value(output_grams, dt);
    }
    else
    {
        clear_stable_phase();
    }

    // activate hpf-hold-switch?
    if (fabs(output_dxdt1) > dxdt_threshold || output_grams > 1000.0 || output_grams < -400.0) // TODO does not work when refilling
    {
        if (!g_input_switch)
        {
            measurement_mark_start_of_event();
            g_input_switch = true;
            g_input_offset = g_prev_hpf1_offsets[9]; // TODO mean/median?
        }

        g_input_switch_timer = 5.0; // must not deactivate for x seconds
    }

    // deactivate switch?
    if (g_input_switch)
    {
        g_input_switch_timer -= dt; // dt
        if (g_input_switch_timer <= 0)
        {
            measurement_mark_end_of_event();
            g_input_switch = false;
            g_hpf1->reset = true;
            g_lpf1->reset = true;
            g_mean1->reset = true;
            g_median1->reset = true;
            g_dxdt1->reset = true;
        }
    }

    return output_grams;
}

