var netPort = 9446;
var serverPassword = "THISISNOTTHEPASSWORD";

var net = require('net'),
    carrier = require('carrier');

var rooms = {};


if (typeof String.prototype.startsWith != 'function') {
  String.prototype.startsWith = function (input){
    return this.substring(0, input.length) === input
  };
} 

var checkMaster = function(roomName, conn) {
    if (rooms[roomName]['currentMaster'] == false) {
        var connList = rooms[roomName]['connQueue'].slice(0);
        while (conn === false || conn.excludeMaster) {
            conn = connList.shift();
            if (conn === undefined) {
                rooms[roomName]['currentMaster'] = false;
                return; // no masters!
            }
            else if (conn !== false && conn.excludeMaster) {
                conn.write('&NOTMASTER\n');
            }
        }
        rooms[roomName]['currentMaster'] = conn;
        conn.write('&NEWMASTER\n');
    } else if (conn !== false) {
        conn.write('&NOTMASTER\n');
    }
}

var passwordTimeout = function(c) {
    c.end("&DISCONNECT TOOK TOO LONG TO LOG IN\n");
};

var server = net.createServer(function(c) {
    console.log('server received connection from', c.remoteAddress);

    c.write('&CONN\n');

    c.passwordTimeout = setTimeout(function() { passwordTimeout(c); }, 5000);
    c.loggedIn = false;

    c.on('close', function() {
        if (!c.hasOwnProperty('room'))
            return;

        var needsNewMaster = false;
        if (rooms[c.room]['currentMaster'] == c) {
             needsNewMaster = true;
        }
        rooms[c.room]['connQueue'] = rooms[c.room]['connQueue'].filter(function (el) { console.log(el !== c); return (el !== c); });
        if (needsNewMaster) {
            rooms[c.room]['currentMaster'] = false;
            if (rooms[c.room]['connQueue'].length !== 0) {
                checkMaster(c.room, false);
            }
        }
    });

    carrier.carry(c, function(line) {
        console.log('got from', c.remoteAddress, '-', line);
        if (line == '&PING') {
            c.write('&PONG\n');
            return;
        }
        else if (!c.loggedIn && line.startsWith('&LOGIN ')) {
            var lineSplit = line.split(" ");
            c.username = lineSplit[1];
            if (line.substring(c.username.length + 2 + "&LOGIN".length) != serverPassword) {
                c.end('&BADPASS\n');
            } else {
                c.loggedIn = true;
                clearTimeout(c.passwordTimeout);
                c.write('&LOGGEDIN\n');
                console.log(c.remoteAddress, "logged in as", c.username);
            }
        }
        else if (!c.loggedIn) {
            return;
        }
        else if (line.startsWith('&JOIN ')) {
            var roomName = line.substring(5);
            if (rooms[roomName] === undefined)
                rooms[roomName] = {'currentMaster': false, 'connQueue': []};
            rooms[roomName]['connQueue'].push(c);
            c.room = roomName;
            checkMaster(roomName, c);
        }

        if (!c.hasOwnProperty('room')) {
            c.write('&NOROOM\n');
            return;
        }

        if (line == '&DROPMASTER' && rooms[c.room]['currentMaster'] == c) {
            c.excludeMaster = true;
            rooms[c.room]['currentMaster'] = false;
            checkMaster(c.room, false);
        }

        if (line[0] == '&') {
            console.log('...message dropped.');
            return;
        }

        rooms[c.room]['connQueue'].forEach(function(elm) { if (elm !== c) elm.write(line + '\n'); });
    });
});

server.listen(netPort, function() {
    console.log('Listening on', 9446);
});
