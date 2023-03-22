#!/bin/bash

touch version.txt

idf.py build

if [ $? -ne 0 ]; then
    echo "Build failed"
    exit -1
fi

echo ""
echo "new version: `cat version.txt`"
echo ""

echo "Updating ..."
time (cat build/cat_scale.bin | nc CatScale 69)

echo "All done."
