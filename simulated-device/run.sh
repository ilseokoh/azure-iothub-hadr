#!/bin/bash
symkey=fUujk4RBEFIFwFPLfcF5OAvbjki85AUZQcf4ZJ4Z7ElyVdjNe/ZAPNaG6mQDFHV+H7hkLAHUugLi2CQvypgpdg==
for i in $(seq 1 100)
do
    devid=krcentral-dev-$i
    echo ${devid}
    node index.js --registrationid ${devid} --symmetrickey ${symkey}
done