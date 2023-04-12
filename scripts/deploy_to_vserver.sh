#!/bin/bash

echo "shutdown running processes ..."
ssh user@vserver "killall CatScale.UI.BlazorServer; killall CatScale.Service"

sleep 2

echo "cleanup screen sessions ..."
ssh user@vserver "echo 'before'; screen -ls; screen -ls | grep 'cs_' | cut -d. -f1 | tr --delete '\t' | xargs -r kill -9; screen -wipe; echo 'after'; screen -ls;"

echo "delete old files ..."
ssh user@vserver "rm apps/ -R; mkdir apps/"

echo "upload archives..."
scp publish/*.tar.gz user@vserver:/home/user/apps

echo "unpack archives..."
ssh user@vserver "cd apps; tar -xzf service.tar.gz; tar -xzf blazorserver.tar.gz;"

echo "copy configs..."
ssh user@vserver "cp secrets/appsettings.json.service apps/service/appsettings.json; cp secrets/appsettings.json.blazorserver apps/blazorserver/appsettings.json"

echo "start service..."
ssh user@vserver "(cd /home/user/apps/service      && screen -d -m -S cs_service      ./CatScale.Service)"

echo "start blazorserver..."
ssh user@vserver "(cd /home/user/apps/blazorserver && screen -d -m -S cs_blazorserver ./CatScale.UI.BlazorServer)"

echo "done"