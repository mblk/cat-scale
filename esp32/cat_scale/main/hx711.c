#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "hx711.h"
#include "time.h"

#include <stdio.h>
#include <string.h>
#include <sys/time.h>

#include "sdkconfig.h"

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <esp_log.h>
#include <esp_timer.h>
#include <driver/gpio.h>



#define PIN_CLOCK   GPIO_NUM_18
#define PIN_DATA    GPIO_NUM_19

static const char *TAG = "hx711";

static portMUX_TYPE g_signal_spinlock = portMUX_INITIALIZER_UNLOCKED;


esp_err_t hx711_init()
{
    ESP_LOGI(TAG, "hx711_init ...");

    // configure pins
    gpio_reset_pin(PIN_CLOCK); // select gpio function, enable pullup and disable input and output
    gpio_reset_pin(PIN_DATA);
    gpio_set_direction(PIN_CLOCK, GPIO_MODE_OUTPUT);
    gpio_set_direction(PIN_DATA, GPIO_MODE_INPUT);

    // reset sensor
    gpio_set_level(PIN_CLOCK, 1);
    esp_rom_delay_us(100);
    gpio_set_level(PIN_CLOCK, 0);
    esp_rom_delay_us(10);

    // read some values
    uint32_t values[10];
    uint32_t t0 = esp_timer_get_time(); // us since boot
    for(int i=0; i<10; i++)
        values[i] = hx711_read_from_sensor();
    uint32_t t1 = esp_timer_get_time(); // us since boot
    
    ESP_LOGI(TAG, "dt for 10 samples: %u", t1 - t0); // should be ~1s
    for(int i=0; i<10; i++)
        ESP_LOGI(TAG, "value %d = %u", i, values[i]);

    return ESP_OK;
}

uint32_t hx711_read_from_sensor()
{
    // Wait for data line to go low. Indicating a sample is ready to be read.
    while(gpio_get_level(PIN_DATA))
    {
        vTaskDelay(1);
    }

    uint32_t data = 0;

    taskENTER_CRITICAL(&g_signal_spinlock); // ~25us
    {
        for (int i=0; i<24; i++)
        {
            gpio_set_level(PIN_CLOCK, 1);
            data <<= 1;
            gpio_set_level(PIN_CLOCK, 0);
            data |= gpio_get_level(PIN_DATA);
        }

        gpio_set_level(PIN_CLOCK, 1);
        data = data ^ 0x00800000; // signed to unsigned
        gpio_set_level(PIN_CLOCK, 0);
    }
    taskEXIT_CRITICAL(&g_signal_spinlock);

    return data;
}
