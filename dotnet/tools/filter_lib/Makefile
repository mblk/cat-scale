CC=gcc
CFLAGS=-c -Wall -Werror -fpic -I ./src/ -DDEBUG_FILTER_CASCADE

LD=gcc
LDFLAGS=-shared

all:
	-rm bin/ -R
	mkdir bin/
	$(CC) $(CFLAGS) src/filter_lib.c                               -o bin/filter_lib.o
	$(CC) $(CFLAGS) ../../../esp32/cat_scale/main/filters.c        -o bin/filters.o
	$(CC) $(CFLAGS) ../../../esp32/cat_scale/main/filter_cascade.c -o bin/filter_cascade.o
	$(LD) $(LDFLAGS) bin/*.o -o bin/filter_lib.so
	-cp bin/filter_lib.so ../CatScale.ReprocessTool/bin/Debug/net7.0/
	-cp bin/filter_lib.so ../CatScale.ReprocessTool/bin/Release/net7.0/
	-cp bin/filter_lib.so ../CatScale.FilterConfigTool/bin/Debug/net7.0/
	-cp bin/filter_lib.so ../CatScale.FilterConfigTool/bin/Release/net7.0/
