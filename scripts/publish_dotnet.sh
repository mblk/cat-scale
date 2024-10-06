#!/bin/bash

#PUBLISH_OPTIONS="-r linux-x64 -c Release --no-self-contained -v minimal"

PUBLISH_OPTIONS="-r linux-x64 -c Release -p:PublishSingleFile=true --self-contained true -v minimal"
PUBLISH_DIR="publish"

if [ -d $PUBLISH_DIR ]; then
    rm $PUBLISH_DIR -R
fi

echo "service..."
dotnet publish ../dotnet/CatScale.Service/CatScale.Service.csproj                 $PUBLISH_OPTIONS -o $PUBLISH_DIR/CatScale.Service/

echo "blazorserver..."
dotnet publish ../dotnet/CatScale.UI.BlazorServer/CatScale.UI.BlazorServer.csproj $PUBLISH_OPTIONS -o $PUBLISH_DIR/CatScale.UI.BlazorServer/

echo "compress..."
cd $PUBLISH_DIR

tar -czf CatScale.Service.tar.gz         CatScale.Service/
tar -czf CatScale.UI.BlazorServer.tar.gz CatScale.UI.BlazorServer/

echo "done"
