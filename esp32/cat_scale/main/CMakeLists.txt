idf_component_register(
    SRCS
    "main.c"
    "wifi.c"
    "http.c"
    "hx711.c"
    "time.c"
    "bme280.c"
    "bme280_user.c"
    "ccs811.c"
    "sensors.c"
    "rc.c"
    "log_udp.c"
    "measurement.c"
    "filters.c"
    "filter_cascade.c"
    "ringbuffer.c"
    INCLUDE_DIRS "")

target_compile_options(${COMPONENT_LIB} PRIVATE "-Wno-format")
