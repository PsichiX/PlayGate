#! /usr/bin/env node

var argv     = process.argv,
    compiler = require('compiler.js'),
    config   = {defines: {}};

// process arguments.
var i, c, arg, off, cmd, param;
for (i = 2, c = argv.length; i < c; ++i){
	arg = argv[i];
	if (arg.substring(0, 2) !== '--' && arg.substring(0, 1) !== '-'){
		config = compiler.readConfigFile(arg);
	} else {
		off = arg.indexOf(':');
		if (off < 0){
			cmd = arg;
			param = null;
		} else {
			cmd = arg.substring(0, off);
			param = arg.substring(off + 1);
		}
		if (cmd === '--verbose' || cmd === '-v'){
			config.verbose = true;
		} else if (cmd === '--entry' || cmd === '-e'){
			config.entry = param;
		} else if (cmd === '--intermediate' || cmd === '-i'){
			config.intermediate = param;
		} else if (cmd === '--output' || cmd === '-o'){
			config.output = param;
		} else if (cmd === '--basedir' || cmd === '-b'){
			config.basedir = param;
		} else if (cmd === '--define' || cmd === '-d'){
			var o = param.indexOf('=');
			if (o < 0){
				config.defines[param] = true;
			} else {
				config.defines[param.substring(0, o)] = JSON.parse(param.substring(o + 1));
			}
		} else if (cmd === '--minify' || cmd === '-m'){
			config.minify = JSON.parse(param);
		}
	}
}

compiler.compile(config);
