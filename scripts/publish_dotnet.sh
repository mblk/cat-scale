#!/bin/bash

PUBLISH_OPTIONS="-r linux-x64 -c Release --no-self-contained -v minimal"
PUBLISH_DIR="publish"

if [ -d $PUBLISH_DIR ]; then
    rm $PUBLISH_DIR -R
fi

echo "service..."
dotnet publish ../dotnet/CatScale.Service/CatScale.Service.csproj                 $PUBLISH_OPTIONS -o $PUBLISH_DIR/service/

echo "blazorserver..."
dotnet publish ../dotnet/CatScale.UI.BlazorServer/CatScale.UI.BlazorServer.csproj $PUBLISH_OPTIONS -o $PUBLISH_DIR/blazorserver/

echo "compress..."
cd $PUBLISH_DIR

tar -czf service.tar.gz service/
tar -czf blazorserver.tar.gz blazorserver/

echo "done"
