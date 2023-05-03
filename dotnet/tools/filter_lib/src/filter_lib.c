#include <stdlib.h>
#include <stdio.h>
#include <assert.h>

typedef void(*start_of_event_handler_t)();
typedef void(*stable_phase_handler_t)(double, double);
typedef void(*end_of_event_handler_t)();

start_of_event_handler_t g_start_handler = NULL;
stable_phase_handler_t g_stable_handler = NULL;
end_of_event_handler_t g_end_handler = NULL;

void register_handlers(start_of_event_handler_t start_handler, stable_phase_handler_t stable_handler, end_of_event_handler_t end_handler)
{
    g_start_handler = start_handler;
    g_stable_handler = stable_handler;
    g_end_handler = end_handler;
}

void measurement_mark_start_of_event(void)
{
    assert(g_start_handler);
    g_start_handler();
}

void measurement_push_stable_phase(double length, double value)
{
    assert(g_stable_handler);
    g_stable_handler(length, value);
}

void measurement_mark_end_of_event(void)
{
    assert(g_end_handler);
    g_end_handler();
}
