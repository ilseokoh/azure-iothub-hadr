'use strict';

var SymmetricKeySecurityClient = require('azure-iot-security-symmetric-key').SymmetricKeySecurityClient;
var ProvisioningDeviceClient = require('azure-iot-provisioning-device').ProvisioningDeviceClient;
var crypto = require('crypto');
var ProvisioningTransport = require('azure-iot-provisioning-device-mqtt').Mqtt;

class DeviceProvision {
    constructor(registerid, symmetrickey, scopeid) {
        this._registerid = registerid;
        this._scopeid = scopeid;
        this._symmetrickey = symmetrickey;
    }

    get hubConnectionString() {
        return (this._connectionString  == '' ? null : this._connectionString);
    }

    computeDerivedSymmetricKey() {
        return crypto.createHmac('SHA256', Buffer.from(this._symmetrickey, 'base64'))
            .update(this._registerid, 'utf8')
            .digest('base64');
    }

    register() {
        var key = this.computeDerivedSymmetricKey();

        var provisioningHost = 'global.azure-devices-provisioning.net';
        var provisioningSecurityClient = new SymmetricKeySecurityClient(this._registerid, key);
        var provisioningClient = ProvisioningDeviceClient.create(provisioningHost, this._scopeid, new ProvisioningTransport(), provisioningSecurityClient);

        console.log(`start registering device: ${this._registerid}`);

        return new Promise((resolve, reject) => {
            provisioningClient.register((err, result) => {
                if (err) {
                    console.log("error registering device: " + err);
                    reject(err);
                } else {
                    console.log('registration succeeded');
                    this._connectionString = 'HostName=' + result.assignedHub + ';DeviceId=' + result.deviceId + ';SharedAccessKey=' + key;
                    resolve(this._connectionString);
                }
            });
        });

    }
}

module.exports = DeviceProvision;