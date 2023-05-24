#!/bin/bash

echo "shutdown running services ..."
ssh user@vserver "sudo systemctl stop CatScale.UI.BlazorServer"
ssh user@vserver "sudo systemctl stop CatScale.Service"

echo "delete old files ..."
ssh user@vserver "rm apps/*.gz; rm apps/CatScale.Service -R; rm apps/CatScale.UI.BlazorServer -R"

echo "upload archives ..."
scp publish/*.tar.gz user@vserver:/home/user/apps

echo "unpack archives ..."
ssh user@vserver "cd apps; tar -xzf CatScale.Service.tar.gz; tar -xzf CatScale.UI.BlazorServer.tar.gz; rm *.gz"

echo "copy configs ..."
ssh user@vserver "cp secrets/CatScale.Service.appsettings.json         apps/CatScale.Service/appsettings.json"
ssh user@vserver "cp secrets/CatScale.UI.BlazorServer.appsettings.json apps/CatScale.UI.BlazorServer/appsettings.json"

echo "copy service files ..."
ssh user@vserver "sudo cp apps/CatScale.Service/CatScale.Service.service                 /etc/systemd/system"
ssh user@vserver "sudo cp apps/CatScale.UI.BlazorServer/CatScale.UI.BlazorServer.service /etc/systemd/system"

echo "reload systemd ..."
ssh user@vserver "sudo systemctl daemon-reload"

echo "start services ..."
ssh user@vserver "sudo systemctl start CatScale.Service"
ssh user@vserver "sudo systemctl start CatScale.UI.BlazorServer"

echo "done"

