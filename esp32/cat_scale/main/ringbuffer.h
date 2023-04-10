#pragma once

#include <stddef.h>
#include <stdbool.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/message_buffer.h>

typedef struct {

    portMUX_TYPE spinlock;

    const size_t item_size;
    const size_t buffer_size_in_items;

    void * const memory;

    size_t write_index;
    size_t read_index;

} ringbuffer_t;


ringbuffer_t *ringbuffer_create(size_t item_size, size_t buffer_size_in_items);
void ringbuffer_destroy(ringbuffer_t *ringbuffer);

void ringbuffer_push(ringbuffer_t *ringbuffer, const void *item);
bool ringbuffer_try_pop(ringbuffer_t *ringbuffer, void *item);
