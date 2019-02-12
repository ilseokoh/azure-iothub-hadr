#!/bin/bash
for i in $(seq 1 100)
do
    devid=krcentral-dev-$i
    echo ${devid}
    node index.js --registrationid ${devid} &
done