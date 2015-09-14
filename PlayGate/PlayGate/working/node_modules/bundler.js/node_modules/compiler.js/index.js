/**
 * Preprocess and compile JavaScript file.
 *
 * @param {Object|String} config configuration JSON object or configuration file path.
 * @param {Boolean} config.verbose determines if program should print not only errors.
 * @param {String} config.entry entry file path.
 * @param {String} config.intermediate intermediate file path.
 * @param {String} config.output output file path.
 * @param {String} config.basedir base directory path (for included files).
 * @param {Object} config.defines map of definitions: {NAME: value}.
 * @param {Object} config.lint determines if lint will be used.
 * @param {Object} config.minify determines if minify will be used.
 * @param {Function|null} callback function called when compilation is complete.
 */
exports.compile = function(config, callback){

	var version          = '1.2.0',
	    fs               = require('fs-extra'),
	    preprocessor     = require('preprocessor'),
	    uglify           = require('uglify-js'),
	    // configuration data.
	    verbose          = false,
	    entryFile        = 'entry.js',
	    intermediateFile = null,
	    distributionFile = null,
	    baseDir          = './',
	    defines          = {},
	    minify           = false;

	callback = callback || function(){
		};

	// process config.
	if (config){
		if (typeof config === 'string'){
			config = exports.readConfigFile(config);
		}
		config.verbose && (verbose = config.verbose);
		config.entry && (entryFile = config.entry);
		config.intermediate && (intermediateFile = config.intermediate);
		config.output && (distributionFile = config.output);
		config.basedir && (baseDir = config.basedir);
		if (config.defines){
			var defs = config.defines,
			    key;
			for (key in defs){
				if (defs.hasOwnProperty(key)){
					defines[key] = defs[key];
				}
			}
		}
		config.minify && (minify = config.minify);
	}

	// configuration validation.
	if (!entryFile){
		throw 'Entry file is not specified!';
	}
	if (!fs.existsSync(entryFile)){
		throw 'Entry file does not exists: ' + entryFile;
	}
	if (!distributionFile){
		distributionFile = entryFile.substring(-3) === '.js'
			? entryFile.substring(0, -3) + 'distribution.js'
			: entryFile + '.distribution';
	}
	if (!baseDir){
		baseDir = './';
	}

	// execute tasks.
	verbose && console.log('Compiler.js v' + version);
	verbose && console.log('>>> Configuration:');
	verbose && console.log('entry: ' + entryFile);
	verbose && intermediateFile && console.log('intermediate: ' + intermediateFile);
	verbose && console.log('output: ' + distributionFile);
	verbose && console.log('basedir: ' + baseDir);
	verbose && console.log('minify: ' + minify);
	verbose && console.log('defines: ' + JSON.stringify(defines, null, '  '));
	verbose && console.log('>>> Performing compilation...');
	var data = fs.readFileSync(entryFile);
	data = new preprocessor(
		data,
		baseDir ? baseDir : '.'
	).process(
		defines ? defines : {}
	);
	if (intermediateFile){
		fs.ensureFileSync(intermediateFile);
		fs.writeFileSync(intermediateFile, data);
	}
	minify && (data = uglify.minify(data, {fromString: true}).code);
	fs.ensureFileSync(distributionFile);
	fs.writeFileSync(distributionFile, data);
	verbose && console.log('>>> Done!');
	callback();

};

exports.readConfigFile = function(path){

	var fs      = require('fs'),
	    content = fs.readFileSync(path);
	if (content){
		var data = JSON.parse(content);
		if (data && data.inherits && typeof data.inherits === 'string'){
			var inherits = exports.readConfigFile(data.inherits),
			    key;
			for (key in data){
				if (data.hasOwnProperty(key)){
					inherits[key] = data[key];
				}
			}
			return inherits;
		}
		return data;
	}
	return null;

};
