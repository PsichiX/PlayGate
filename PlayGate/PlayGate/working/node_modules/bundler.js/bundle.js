#! /usr/bin/env node

var argv    = process.argv,
    bundler = require('bundler.js'),
    config  = null,
    mode    = null;

// process arguments.
if (argv.length > 2){
	config = argv[2];
}
if (argv.length > 3){
	mode = argv[3];
}

bundler.bundle(config, null, mode);
