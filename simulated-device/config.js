'use strict'

var fs = require('fs');

class ConnectoinConfig { 
    constructor(registerid) { 
        let dir = `./device-config`;
        this._filename = `${dir}/${registerid}`;

        if (!fs.existsSync(dir)) { 
            fs.mkdirSync(dir)
        }
    }

    read() { 
        return new Promise((resolve, reject) => {
            fs.readFile(this._filename, 'utf-8', (error, data) => { 
                if (error) { 
                    reject(error); 
                }
                if (data == null || data == '') { 
                    reject(error); 
                }
                resolve(data);
            });
        });
    }

    save(connectionString) { 
        return new Promise((resolve, reject) => { 
            fs.writeFile(this._filename, connectionString, 'utf-8', function(err) {
                if (err) {
                    reject(err);
                } else { 
                    resolve();
                }
            });
        });
    }
}

module.exports = ConnectoinConfig;