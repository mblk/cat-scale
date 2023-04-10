#undef __linux__ // BUG: https://github.com/microsoft/vscode-cpptools/issues/9680

#include "ringbuffer.h"

#include <stdio.h>
#include <string.h>
#include <math.h>

#include "sdkconfig.h"

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>

#include <esp_log.h>

static const char *TAG = "ringbuffer";

ringbuffer_t *ringbuffer_create(size_t item_size, size_t buffer_size_in_items)
{
    assert(item_size);
    assert(buffer_size_in_items);

    const size_t total_size = item_size * buffer_size_in_items;

    void * const memory = malloc(total_size);
    assert(memory);

    const ringbuffer_t ringbuffer_config = {
        .spinlock = portMUX_INITIALIZER_UNLOCKED,
        .item_size = item_size,
        .buffer_size_in_items = buffer_size_in_items,
        .memory = memory,
        .read_index = 0,
        .write_index = 0,
    };

    ringbuffer_t * const ringbuffer = malloc(sizeof(ringbuffer_t));
    assert(ringbuffer);
    memcpy(ringbuffer, &ringbuffer_config, sizeof(ringbuffer_t));

    return ringbuffer;
}

void ringbuffer_destroy(ringbuffer_t *ringbuffer)
{
    assert(ringbuffer);

    free(ringbuffer->memory);
    free(ringbuffer);
}

void ringbuffer_push(ringbuffer_t *ringbuffer, const void *item)
{
    assert(ringbuffer);
    assert(item);

    bool overflow = false;

    taskENTER_CRITICAL(&ringbuffer->spinlock);
    {
        void * const target = ringbuffer->memory + ringbuffer->item_size * ringbuffer->write_index;
        memcpy(target, item, ringbuffer->item_size);
        ringbuffer->write_index = (ringbuffer->write_index + 1) % ringbuffer->buffer_size_in_items;
        overflow = ringbuffer->write_index == ringbuffer->read_index;
    }
    taskEXIT_CRITICAL(&ringbuffer->spinlock);

    if (overflow) ESP_LOGE(TAG, "ring buffer overflow");
}

bool ringbuffer_try_pop(ringbuffer_t *ringbuffer, void *item)
{
    assert(ringbuffer);
    assert(item);

    bool ret = false;

    taskENTER_CRITICAL(&ringbuffer->spinlock);
    {
        if (ringbuffer->write_index != ringbuffer->read_index)
        {
            const void * const source = ringbuffer->memory + ringbuffer->item_size * ringbuffer->read_index;
            memcpy(item, source, ringbuffer->item_size);
            ringbuffer->read_index = (ringbuffer->read_index + 1) % ringbuffer->buffer_size_in_items;
            ret = true;
        }
    }
    taskEXIT_CRITICAL(&ringbuffer->spinlock);

    return ret;
}