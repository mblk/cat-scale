#pragma once

void filter_cascade_init(void);
void filter_cascade_cleanup(void);

double filter_cascade_process(double input, double dt);

#if DEBUG_FILTER_CASCADE
void filter_cascade_debug(const char *id, double value);
#endif
