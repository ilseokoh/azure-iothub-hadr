'use strict';

var Protocol = require('azure-iot-device-mqtt').Mqtt;
var Client = require('azure-iot-device').Client;
var Message = require('azure-iot-device').Message;

var DeviceProvision = require('./provision');
var ConnectionConfig = require('./config');

// Parse args
var argv = require('yargs')
  .usage('Usage: $0 --registrationid <DEVICE ID> --symmetrickey <GROUP SYMMETRIC KEY> ')
  .option('registrationid', {
    alias: 'id',
    describe: 'provisioning registration id',
    type: 'string',
    demandOption: true
  })
  .option('symmetrickey', {
    alias: 'k',
    describe: 'provisioning symmetric key',
    type: 'string',
    demandOption: true
  })
  .argv;

var registrationId = argv.registrationid;
var symmetricKey = argv.symmetrickey;
var idScope = '0ne00045D0E';
var connectionString = '';
var cnt = 0;
var hubClient = null;
var hubHostname = '';
var sendHeartbeatInterval = null;

// Device Provision
const provision = new DeviceProvision(registrationId, symmetricKey, idScope);

// IoT Hub Connection String File save 
const connectionStringInfo = new ConnectionConfig(registrationId);

// register device to DPS
function registerDevice() {
  // Register device
  var registerPromise = provision.register();

  registerPromise.then((result) => {
    console.log(`connectionString: ${result}`);
    connectionString = result;

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
  // Reprovision
  registerDevice();

  // complete the response
  response.send(200, 'reprovision', function(err) {
      if(!!err) {
          console.error('An error ocurred when sending a method response:\n' +
              err.toString());
      } else {
          console.log('Response to method \'' + request.methodName +
              '\' sent successfully.' );
      }
  });
}

function getHubHostname(connstr) { 
  var arr = connstr.split(';');
  var hostname = arr.filter(str => str.startsWith('HostName='))[0];
  var name = hostname.split('=')[1];
  return name;
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
      clientInformation.removeAllListener();

      // call backup channel to get current situation. 

      
    });

    hubClient.on('connect', () => {
      console.log('client is connectted');
    });

    hubClient.on('error', () => {
      console.log('error');
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
    
    twin.properties.reported.update(reportedProperties, function(error) {
      if (error) { console.error('twin reported error')}
      console.log('twin reported.')
    });
  }
}

function deviceStart() {
  hubHostname = getHubHostname(connectionString);

  hubClient = Client.fromConnectionString(connectionString, Protocol);
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
  deviceStart();
});