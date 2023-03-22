#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "time.h"

#include <stdio.h>

#include "esp_err.h"
#include "esp_log.h"
#include "esp_timer.h"
#include "esp_sntp.h"

static const char *TAG = "time";

esp_err_t time_init_and_sync()
{
    ESP_LOGI(TAG, "time_init");

    sntp_setoperatingmode(SNTP_OPMODE_POLL);
    //sntp_setservername(0, "pool.ntp.org");
    sntp_setservername(0, "fritz.box");
    //sntp_set_sync_mode(SNTP_SYNC_MODE_SMOOTH);
    sntp_set_sync_mode(SNTP_SYNC_MODE_IMMED);
    sntp_set_sync_interval(15 * 1000);
    sntp_init();

    int64_t sntp_init_t0 = esp_timer_get_time();

    while(true)
    {
        sntp_sync_status_t sync_status = sntp_get_sync_status();
        if (sync_status == SNTP_SYNC_STATUS_COMPLETED)
            break;

        vTaskDelay(100 / portTICK_PERIOD_MS);
    }

    int64_t sntp_init_t1 = esp_timer_get_time();
    int64_t sntp_init_dt = sntp_init_t1 - sntp_init_t0;
    int sntp_init_dt_ms = (int)(sntp_init_dt / 1000);

    ESP_LOGI(TAG, "sntp init completed in %d ms", sntp_init_dt_ms);

    struct timeval tv;
    gettimeofday(&tv, NULL);
    char time_buffer[128] = {};
    convert_timeval_to_iso8601(tv, time_buffer, sizeof(time_buffer));
    ESP_LOGI(TAG, "Time: %s", time_buffer);

    return ESP_OK;
}

#if 0
void gettime_iso8601(char *output, size_t output_size)
{
    struct timeval tv;
    struct tm tm;
    //char timestamp[] = "YYYY-MM-ddTHH:mm:ss.SSS+0000";

    /* Get the current time at high precision; could also use clock_gettime() for
    * even higher precision times if we want it. */
    gettimeofday(&tv, NULL);

    /* convert to time to 'struct tm' for use with strftime */
    localtime_r(&tv.tv_sec, &tm);

    /* format the time */
    //strftime(timestamp, sizeof(timestamp), "%Y-%m-%dT%H:%M:%S.000%z", &tm);
    strftime(output, output_size, "%Y-%m-%dT%H:%M:%S.000%z", &tm);

    /* but, since strftime() can't subsecond precision, we have to hack it
    * in manually. '20' is the string offset of the subsecond value in our
    * timestamp string. Also, because sprintf always writes a null, we have to 
    * write the subsecond value as well as the rest of the string already there.
    */
    //sprintf(timestamp + 20, "%03ld%s", tv.tv_usec / 1000, timestamp + 23);
    sprintf(output + 20, "%03ld", tv.tv_usec / 1000);

    if (output_size > 25)
    {
        snprintf(output + 20, output_size - 20, "%03ld", tv.tv_usec / 1000);

        output[23] = 'Z';
        output[24] = 0;
    }

    // timestamp[23] = '+'; // Doesn't work for .net
    // timestamp[24] = '0';
    // timestamp[25] = '0';
    // timestamp[26] = '0';
    // timestamp[27] = '0';
    // timestamp[28] = 0;

    /* print the timestamp */
    //printf("foofoofoo: '%s'\n", timestamp);
}
#endif

void convert_timeval_to_iso8601(struct timeval tv, char *output, size_t output_size)
{
    struct tm tm;

    //localtime_r(&tv.tv_sec, &tm);
    gmtime_r(&tv.tv_sec, &tm);

    strftime(output, output_size, "%Y-%m-%dT%H:%M:%S.000%z", &tm);

    sprintf(output + 20, "%03ld", tv.tv_usec / 1000);

    if (output_size > 25)
    {
        snprintf(output + 20, output_size - 20, "%03ld", tv.tv_usec / 1000);

        output[23] = 'Z';
        output[24] = 0;
    }
}
