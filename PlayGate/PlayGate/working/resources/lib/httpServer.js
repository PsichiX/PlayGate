var args 		= process.argv,
	connect     = require('connect'),
    serveStatic = require('serve-static'),
    port 		= 8080;

if(args.length > 1){
	port = parseInt(args[args.length - 1], 10);
}

connect().use(serveStatic(process.cwd())).listen(port);

console.log('PlayGate local server running on port: ' + port);
