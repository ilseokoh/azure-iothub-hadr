'use strict';

var Protocol = require('azure-iot-device-mqtt').Mqtt;
var Client = require('azure-iot-device').Client;
var Message = require('azure-iot-device').Message;
var axios = require('axios');
var NoRetry = require('azure-iot-common').NoRetry;

var DeviceProvision = require('./provision');
var ConnectionStringFileManager = require('./config-manager');
var config = require('./config.json');

// Parse args
var argv = require('yargs')
  .usage('Usage: $0 --registrationid <DEVICE ID>')
  .option('registrationid', {
    alias: 'id',
    describe: 'provisioning registration id',
    type: 'string',
    demandOption: true
  })
  .argv;

var registrationId = argv.registrationid;
var idScope = config.idScope;
var connectionString = '';
var cnt = 0;
var hubClient = null;
var hubHostname = '';
var sendHeartbeatInterval = null;

// Device Provision 1st
var provision = new DeviceProvision(registrationId, config.mainIoTHub, idScope);

// IoT Hub Connection String File save 
var connectionStringInfo = new ConnectionStringFileManager(registrationId);

// register device to DPS
function registerDevice() {
  // Register device
  var registerPromise = provision.register();

  registerPromise.then((result) => {
    console.log(`connectionString: ${result}`);
    connectionString = result;

    deviceStart();

    // save connection string to file 
    connectionStringInfo.save(result).then((saveresult) => {
      console.log(`connection string saved.`);
    }, (saveerr) => {
      console.error(`Error: save connection string: ${saveerr}`);
    });
  }, (error) => {
    console.error(`DPS register error: ${error}`);
  });
}

function onReprovision(request, response) {
  // delete config file first 
  connectionStringInfo.delete();

  // Device Provision 2nd
  provision = new DeviceProvision(registrationId, config.secondaryIoTHub, idScope);

  // Reprovision
  registerDevice();

  // complete the response
  response.send(200, 'reprovision', function (err) {
    if (!!err) {
      console.error('An error ocurred when sending a method response:\n' +
        err.toString());
    } else {
      console.log('Response to method \'' + request.methodName +
        '\' sent successfully.');
    }
  });
}

function getHubHostname(connstr) {
  var arr = connstr.split(';');
  var hostname = arr.filter(str => str.startsWith('HostName='))[0];
  var name = hostname.split('=')[1];
  return name;
}

function getIoTHubStatus() {
  axios.get(config.checkIoTHealthUrl)
    .then(response => {
      var healthy = response.data.healthy;

      console.log(`healthy: ${healthy}`);

      if (healthy == false) {

        // delete config file first 
        connectionStringInfo.delete();

        // time to reprovision
        // Device Provision 2nd
        console.log(`Reprovision now`);
        provision = new DeviceProvision(registrationId, config.secondaryIoTHub, idScope);
        registerDevice();
      }
    })
    .catch(error => {
      console.log(error);
    });
}

var connectCallback = function (err) {
  if (err) {
    console.error(`Could not connect ${err.message}`);
  } else {
    console.log('Client connected');
    hubClient.onDeviceMethod('reprovision', onReprovision);

    hubClient.on('disconnect', () => {
      console.log('client on disconnect called.');

      clearInterval(sendHeartbeatInterval);

      // call backup channel to get current situation. 
      getIoTHubStatus();

      // start device again
      deviceStart();

    });

    hubClient.on('connect', () => {
      console.log('client is connectted');

      cnt = 0;

      sendHeartbeatInterval = setInterval(function () {
        cnt += 1;
        var data = {
          'hubHostname': hubHostname,
          'count': cnt
        };
        var payload = JSON.stringify(data);
        var message = new Message(payload);
        hubClient.sendEvent(message);
      }, 3000);

    });

    sendHeartbeatInterval = setInterval(function () {
      cnt += 1;
      var data = {
        'hubHostname': hubHostname,
        'count': cnt
      };
      var payload = JSON.stringify(data);
      var message = new Message(payload);
      hubClient.sendEvent(message);
    }, 3000);

    hubClient.on('error', () => {
      console.log('error');
    });
    
  }
}

function twinCallback(err, twin) {
  if (err) {
    console.error('could not get twin');
  } else {
    console.log('twin created');
    var reportedProperties = {
      "hubHostName": hubHostname
    }

    twin.properties.reported.update(reportedProperties, function (error) {
      if (error) { console.error('twin reported error') }
      console.log('twin reported.')
    });
  }
}

function deviceStart() {
  hubHostname = getHubHostname(connectionString);

  hubClient = Client.fromConnectionString(connectionString, Protocol);
  hubClient.setRetryPolicy(new NoRetry());
  hubClient.open(connectCallback);

  hubClient.getTwin(twinCallback);
}

// Read connection string from file first. 
connectionStringInfo.read().then((data) => {
  console.log(`read connection string successfully.`);
  connectionString = data;
  deviceStart();

}, (err) => {
  console.log(`can't find connection string. start provisioning`);
  registerDevice();
});