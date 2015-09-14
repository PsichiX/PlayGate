/**
 * Produces bundle that contains files matching to provided variants.
 *
 * @param {Object|String} config configuration JSON object or configuration file path.
 * @param {Boolean} config.verbose determines if program should print not only errors.
 * @param {String|Object} config.source source files base dir.
 * @param {String|Object} config.intermediate intermediate files base dir.
 * @param {String|Object} config.destination destination files base dir.
 * @param {Object} config.compiler compiler configuration (per mode or global).
 * @param {Array} config.files list of files (you can specify action and variants (as logical expression) in path: "main.js @compile|something ? (code & html)").
 * @param {Array|Object} config.variants list of variants available for given bundle (per mode using map: {"mode": ["variant"]} or global using array: ["variant"]).
 * @param {Object|null} actions map of custom actions as functions: function(fileObject, bundleConfigObject, processedFileInfo:{path, pathTo, id, action, variants}, bundlerActions){}.
 * @param {String|null} mode name of mode used to produce bundle.
 */
exports.bundle = function(config, actions, mode){

	var version             = '1.0.13',
	    fs                  = require('fs-extra'),
	    path                = require('path'),
	    compiler            = require('compiler.js'),
	    verbose             = false,
	    sourceBaseDir       = '.',
	    intermediateBaseDir = null,
	    destinationBaseDir  = '.',
	    compilerOptions     = null,
	    files               = null,
	    variants            = null;

	// process config.
	if (config){
		if (typeof config === 'string'){
			config = exports.readConfigFile(config);
		}
		config.verbose && (verbose = config.verbose);
		config.source && (sourceBaseDir = config.source);
		config.intermediate && (intermediateBaseDir = config.intermediate);
		config.destination && (destinationBaseDir = config.destination);
		config.compiler && (compilerOptions = config.compiler);
		config.files && (files = config.files);
		config.variants && (variants = config.variants);

		if (typeof verbose === 'object'){
			if (typeof mode !== 'string'){
				throw '`mode` must be specified to process this bundle!';
			}
			if (verbose.hasOwnProperty(mode)){
				verbose = verbose[mode];
			} else {
				throw 'Configuration `verbose` option is not specified for mode: ' + mode;
			}
		}
		if (typeof sourceBaseDir === 'object'){
			if (typeof mode !== 'string'){
				throw '`mode` must be specified to process this bundle!';
			}
			if (sourceBaseDir.hasOwnProperty(mode)){
				sourceBaseDir = sourceBaseDir[mode];
			} else {
				throw 'Configuration `source` option is not specified for mode: ' + mode;
			}
		}
		if (intermediateBaseDir && typeof intermediateBaseDir === 'object'){
			if (typeof mode !== 'string'){
				throw '`mode` must be specified to process this bundle!';
			}
			if (intermediateBaseDir.hasOwnProperty(mode)){
				intermediateBaseDir = intermediateBaseDir[mode];
			} else {
				throw 'Configuration `intermediate` option is not specified for mode: ' + mode;
			}
		}
		if (typeof destinationBaseDir === 'object'){
			if (typeof mode !== 'string'){
				throw '`mode` must be specified to process this bundle!';
			}
			if (destinationBaseDir.hasOwnProperty(mode)){
				destinationBaseDir = destinationBaseDir[mode];
			} else {
				throw 'Configuration `destination` option is not specified for mode: ' + mode;
			}
		}
		if (typeof mode === 'string'){
			if (compilerOptions.hasOwnProperty(mode)){
				compilerOptions = compilerOptions[mode];
			} else {
				throw 'Configuration `compiler` option is not specified for mode: ' + mode;
			}
		}
		if (typeof variants !== 'array'){
			if (variants.hasOwnProperty(mode)){
				variants = variants[mode];
			} else {
				throw 'Configuration `variants` option is not specified for mode: ' + mode;
			}
		}
	}

	// configuration validation.
	if (!files || files.length <= 0){
		throw 'Files are not specified!';
	}
	if (!sourceBaseDir || !fs.existsSync(sourceBaseDir)){
		throw 'source base directory is not specified or does not exists: ' + sourceBaseDir;
	}
	if (!destinationBaseDir){
		throw 'destination base directory is not specified!';
	}
	sourceBaseDir.length > 0 && sourceBaseDir[sourceBaseDir.length - 1] !== '/' && (sourceBaseDir += '/');
	intermediateBaseDir && intermediateBaseDir.length > 0 && intermediateBaseDir[intermediateBaseDir.length - 1] !== '/' && (intermediateBaseDir += '/');
	destinationBaseDir.length > 0 && destinationBaseDir[destinationBaseDir.length - 1] !== '/' && (destinationBaseDir += '/');

	// perform bundle.
	var bundlerActions = {
		    copy: function(file, config, info, bundleDirs){
			    var cf = bundleDirs.source + info.path,
			        ct = bundleDirs.destination + info.pathTo;
			    if (fs.existsSync(cf)){
				    fs.copySync(cf, ct, {clobber: true});
				    verbose && console.log('File copied: ' + cf + ' -> ' + ct);
			    } else {
				    console.error('File does not exists: ' + JSON.stringify(info.file));
			    }
		    },
		    compile: function(file, config, info, bundleDirs){
			    var cfg = {
				    verbose: verbose,
				    entry: bundleDirs.source + info.path,
				    intermediate: bundleDirs.intermediate ? bundleDirs.intermediate + info.pathTo : null,
				    output: bundleDirs.destination + info.pathTo,
				    basedir: bundleDirs.source
			    };
			    if (compilerOptions){
				    compilerOptions.hasOwnProperty('defines') && (cfg.defines = compilerOptions.defines);
				    compilerOptions.hasOwnProperty('minify') && (cfg.minify = compilerOptions.minify);
			    }
			    fs.mkdirsSync(path.dirname(cfg.output));
			    compiler.compile(cfg);
		    }
	    },
	    bundleDirs     = {
		    source: sourceBaseDir,
		    intermediate: intermediateBaseDir,
		    destination: destinationBaseDir
	    };
	verbose && console.log('Bundler.js v' + version);
	verbose && mode && console.log('>>> Mode: ' + mode);
	verbose && console.log('>>> Source base dir: ' + sourceBaseDir);
	verbose && console.log('>>> Destination base dir: ' + destinationBaseDir);
	verbose && console.log('>>> Compiler options: ' + JSON.stringify(compilerOptions));
	verbose && console.log('>>> Variants: ' + JSON.stringify(variants));
	var i, c, f, oa, ov, op, pf, pt, id, a, v;
	for (i = 0, c = files.length; i < c; ++i){
		f = files[i];
		if (typeof f === 'string'){
			oa = f.indexOf('@');
			ov = f.indexOf('?');
			if (oa < 0 && ov < 0){
				pf = f;
				id = f;
				a = 'copy';
				v = null;
			} else {
				if (ov >= 0 && oa < 0){
					pf = f.substring(0, ov);
					id = pf;
					a = 'copy';
					v = f.substring(ov + 1);
				} else if (oa >= 0 && ov < 0){
					pf = f.substring(0, oa);
					id = pf;
					a = f.substring(oa + 1);
					v = null;
				} else {
					if (oa < ov){
						pf = f.substring(0, oa);
						id = pf;
						a = f.substring(oa + 1, ov);
						v = f.substring(ov + 1);
					} else {
						pf = f.substring(0, ov);
						id = pf;
						a = f.substring(oa + 1);
						v = f.substring(ov + 1, oa);
					}
				}
			}
		} else {
			if (typeof f.path === 'string'){
				pf = f.path;
			} else {
				console.error('Cannot resolve file path or action of: ' + JSON.stringify(f));
				continue;
			}
			id = typeof f.id === 'string' ? f.id : pf;
			a = typeof f.action === 'string' ? f.action : 'copy';
			v = typeof f.variants === 'string' ? f.variants : null;
		}
		op = pf.indexOf(':');
		if (op < 0){
			pt = pf;
		} else {
			pt = pf.substring(op + 1);
			pf = pf.substring(0, op);
			id = pt;
		}
		pf && (pf = pf.trim());
		pt && (pt = pt.trim());
		id && (id = id.trim());
		a && (a = a.trim());
		v && (v = v.trim());
		if (v && v.length > 0 && variants && variants.length > 0 && !exports.checkVariants(v, variants)){
			verbose && console.warn('File does not match variants: ' + JSON.stringify(f));
			continue;
		}
		var aa = a.split('|'),
		    ai, ac;
		for (ai = 0, ac = aa.length; ai < ac; ++ai){
			a = aa[ai].trim();
			var info = {path: pf, pathTo: pt, id: id, action: a, variants: v};
			if (actions && actions[a] && actions[a] instanceof Function){
				actions[a](f, config, info, bundleDirs, bundlerActions);
			} else if (bundlerActions[a]){
				bundlerActions[a](f, config, info, bundleDirs);
			} else {
				console.error('Cannot resolve file action: ' + JSON.stringify(f));
			}
		}
	}

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

exports.checkVariants = function(input, variants){
	/*
	 * Generated by PEG.js 0.8.0.
	 *
	 * http://pegjs.majda.cz/
	 */

	function peg$subclass(child, parent){
		function ctor(){
			this.constructor = child;
		}

		ctor.prototype = parent.prototype;
		child.prototype = new ctor();
	}

	function SyntaxError(message, expected, found, offset, line, column){
		this.message = message;
		this.expected = expected;
		this.found = found;
		this.offset = offset;
		this.line = line;
		this.column = column;

		this.name = "SyntaxError";
	}

	peg$subclass(SyntaxError, Error);

	function parse(input){
		var options                = arguments.length > 1 ? arguments[1] : {},

		    peg$FAILED             = {},

		    peg$startRuleFunctions = {start: peg$parsestart},
		    peg$startRuleFunction  = peg$parsestart,

		    peg$c0                 = [],
		    peg$c1                 = /^[ \t]/,
		    peg$c2                 = {type: "class", value: "[ \\t]", description: "[ \\t]"},
		    peg$c3                 = peg$FAILED,
		    peg$c4                 = "&",
		    peg$c5                 = {type: "literal", value: "&", description: "\"&\""},
		    peg$c6                 = function(ws0, left, right, ws1){
			    return left && right;
		    },
		    peg$c7                 = "|",
		    peg$c8                 = {type: "literal", value: "|", description: "\"|\""},
		    peg$c9                 = function(ws0, left, right, ws1){
			    return left || right;
		    },
		    peg$c10                = "!",
		    peg$c11                = {type: "literal", value: "!", description: "\"!\""},
		    peg$c12                = function(ws0, value, ws1){
			    return !value;
		    },
		    peg$c13                = "(",
		    peg$c14                = {type: "literal", value: "(", description: "\"(\""},
		    peg$c15                = ")",
		    peg$c16                = {type: "literal", value: ")", description: "\")\""},
		    peg$c17                = function(ws0, op_and, ws1){
			    return op_and;
		    },
		    peg$c18                = {type: "other", description: "variant"},
		    peg$c19                = /^[_a-zA-Z0-9]/,
		    peg$c20                = {type: "class", value: "[_a-zA-Z0-9]", description: "[_a-zA-Z0-9]"},
		    peg$c21                = function(ws0, name, ws1){
			    return variantExists(name);
		    },

		    peg$currPos            = 0,
		    peg$reportedPos        = 0,
		    peg$cachedPos          = 0,
		    peg$cachedPosDetails   = {line: 1, column: 1, seenCR: false},
		    peg$maxFailPos         = 0,
		    peg$maxFailExpected    = [],
		    peg$silentFails        = 0,

		    peg$result;

		if ("startRule" in options){
			if (!(options.startRule in peg$startRuleFunctions)){
				throw new Error("Can't start parsing from rule \"" + options.startRule + "\".");
			}

			peg$startRuleFunction = peg$startRuleFunctions[options.startRule];
		}

		function text(){
			return input.substring(peg$reportedPos, peg$currPos);
		}

		function offset(){
			return peg$reportedPos;
		}

		function line(){
			return peg$computePosDetails(peg$reportedPos).line;
		}

		function column(){
			return peg$computePosDetails(peg$reportedPos).column;
		}

		function expected(description){
			throw peg$buildException(
				null,
				[{type: "other", description: description}],
				peg$reportedPos
			);
		}

		function error(message){
			throw peg$buildException(message, null, peg$reportedPos);
		}

		function peg$computePosDetails(pos){
			function advance(details, startPos, endPos){
				var p, ch;

				for (p = startPos; p < endPos; p++){
					ch = input.charAt(p);
					if (ch === "\n"){
						if (!details.seenCR){
							details.line++;
						}
						details.column = 1;
						details.seenCR = false;
					} else if (ch === "\r" || ch === "\u2028" || ch === "\u2029"){
						details.line++;
						details.column = 1;
						details.seenCR = true;
					} else {
						details.column++;
						details.seenCR = false;
					}
				}
			}

			if (peg$cachedPos !== pos){
				if (peg$cachedPos > pos){
					peg$cachedPos = 0;
					peg$cachedPosDetails = {line: 1, column: 1, seenCR: false};
				}
				advance(peg$cachedPosDetails, peg$cachedPos, pos);
				peg$cachedPos = pos;
			}

			return peg$cachedPosDetails;
		}

		function peg$fail(expected){
			if (peg$currPos < peg$maxFailPos){
				return;
			}

			if (peg$currPos > peg$maxFailPos){
				peg$maxFailPos = peg$currPos;
				peg$maxFailExpected = [];
			}

			peg$maxFailExpected.push(expected);
		}

		function peg$buildException(message, expected, pos){
			function cleanupExpected(expected){
				var i = 1;

				expected.sort(function(a, b){
					if (a.description < b.description){
						return -1;
					} else if (a.description > b.description){
						return 1;
					} else {
						return 0;
					}
				});

				while (i < expected.length){
					if (expected[i - 1] === expected[i]){
						expected.splice(i, 1);
					} else {
						i++;
					}
				}
			}

			function buildMessage(expected, found){
				function stringEscape(s){
					function hex(ch){
						return ch.charCodeAt(0).toString(16).toUpperCase();
					}

					return s
						.replace(/\\/g, '\\\\')
						.replace(/"/g, '\\"')
						.replace(/\x08/g, '\\b')
						.replace(/\t/g, '\\t')
						.replace(/\n/g, '\\n')
						.replace(/\f/g, '\\f')
						.replace(/\r/g, '\\r')
						.replace(/[\x00-\x07\x0B\x0E\x0F]/g, function(ch){
							return '\\x0' + hex(ch);
						})
						.replace(/[\x10-\x1F\x80-\xFF]/g, function(ch){
							return '\\x' + hex(ch);
						})
						.replace(/[\u0180-\u0FFF]/g, function(ch){
							return '\\u0' + hex(ch);
						})
						.replace(/[\u1080-\uFFFF]/g, function(ch){
							return '\\u' + hex(ch);
						});
				}

				var expectedDescs = new Array(expected.length),
				    expectedDesc, foundDesc, i;

				for (i = 0; i < expected.length; i++){
					expectedDescs[i] = expected[i].description;
				}

				expectedDesc = expected.length > 1
					? expectedDescs.slice(0, -1).join(", ")
				+ " or "
				+ expectedDescs[expected.length - 1]
					: expectedDescs[0];

				foundDesc = found ? "\"" + stringEscape(found) + "\"" : "end of input";

				return "Expected " + expectedDesc + " but " + foundDesc + " found.";
			}

			var posDetails = peg$computePosDetails(pos),
			    found      = pos < input.length ? input.charAt(pos) : null;

			if (expected !== null){
				cleanupExpected(expected);
			}

			return new SyntaxError(
				message !== null ? message : buildMessage(expected, found),
				expected,
				found,
				pos,
				posDetails.line,
				posDetails.column
			);
		}

		function peg$parsestart(){
			var s0;

			s0 = peg$parseop_and();

			return s0;
		}

		function peg$parsews(){
			var s0, s1;

			s0 = [];
			if (peg$c1.test(input.charAt(peg$currPos))){
				s1 = input.charAt(peg$currPos);
				peg$currPos++;
			} else {
				s1 = peg$FAILED;
				if (peg$silentFails === 0){
					peg$fail(peg$c2);
				}
			}
			while (s1 !== peg$FAILED){
				s0.push(s1);
				if (peg$c1.test(input.charAt(peg$currPos))){
					s1 = input.charAt(peg$currPos);
					peg$currPos++;
				} else {
					s1 = peg$FAILED;
					if (peg$silentFails === 0){
						peg$fail(peg$c2);
					}
				}
			}

			return s0;
		}

		function peg$parseop_and(){
			var s0, s1, s2, s3, s4, s5;

			s0 = peg$currPos;
			s1 = peg$parsews();
			if (s1 !== peg$FAILED){
				s2 = peg$parseop_or();
				if (s2 !== peg$FAILED){
					if (input.charCodeAt(peg$currPos) === 38){
						s3 = peg$c4;
						peg$currPos++;
					} else {
						s3 = peg$FAILED;
						if (peg$silentFails === 0){
							peg$fail(peg$c5);
						}
					}
					if (s3 !== peg$FAILED){
						s4 = peg$parseop_and();
						if (s4 !== peg$FAILED){
							s5 = peg$parsews();
							if (s5 !== peg$FAILED){
								peg$reportedPos = s0;
								s1 = peg$c6(s1, s2, s4, s5);
								s0 = s1;
							} else {
								peg$currPos = s0;
								s0 = peg$c3;
							}
						} else {
							peg$currPos = s0;
							s0 = peg$c3;
						}
					} else {
						peg$currPos = s0;
						s0 = peg$c3;
					}
				} else {
					peg$currPos = s0;
					s0 = peg$c3;
				}
			} else {
				peg$currPos = s0;
				s0 = peg$c3;
			}
			if (s0 === peg$FAILED){
				s0 = peg$parseop_or();
			}

			return s0;
		}

		function peg$parseop_or(){
			var s0, s1, s2, s3, s4, s5;

			s0 = peg$currPos;
			s1 = peg$parsews();
			if (s1 !== peg$FAILED){
				s2 = peg$parseop_not();
				if (s2 !== peg$FAILED){
					if (input.charCodeAt(peg$currPos) === 124){
						s3 = peg$c7;
						peg$currPos++;
					} else {
						s3 = peg$FAILED;
						if (peg$silentFails === 0){
							peg$fail(peg$c8);
						}
					}
					if (s3 !== peg$FAILED){
						s4 = peg$parseop_or();
						if (s4 !== peg$FAILED){
							s5 = peg$parsews();
							if (s5 !== peg$FAILED){
								peg$reportedPos = s0;
								s1 = peg$c9(s1, s2, s4, s5);
								s0 = s1;
							} else {
								peg$currPos = s0;
								s0 = peg$c3;
							}
						} else {
							peg$currPos = s0;
							s0 = peg$c3;
						}
					} else {
						peg$currPos = s0;
						s0 = peg$c3;
					}
				} else {
					peg$currPos = s0;
					s0 = peg$c3;
				}
			} else {
				peg$currPos = s0;
				s0 = peg$c3;
			}
			if (s0 === peg$FAILED){
				s0 = peg$parseop_not();
			}

			return s0;
		}

		function peg$parseop_not(){
			var s0, s1, s2, s3, s4;

			s0 = peg$currPos;
			s1 = peg$parsews();
			if (s1 !== peg$FAILED){
				if (input.charCodeAt(peg$currPos) === 33){
					s2 = peg$c10;
					peg$currPos++;
				} else {
					s2 = peg$FAILED;
					if (peg$silentFails === 0){
						peg$fail(peg$c11);
					}
				}
				if (s2 !== peg$FAILED){
					s3 = peg$parseprimary();
					if (s3 !== peg$FAILED){
						s4 = peg$parsews();
						if (s4 !== peg$FAILED){
							peg$reportedPos = s0;
							s1 = peg$c12(s1, s3, s4);
							s0 = s1;
						} else {
							peg$currPos = s0;
							s0 = peg$c3;
						}
					} else {
						peg$currPos = s0;
						s0 = peg$c3;
					}
				} else {
					peg$currPos = s0;
					s0 = peg$c3;
				}
			} else {
				peg$currPos = s0;
				s0 = peg$c3;
			}
			if (s0 === peg$FAILED){
				s0 = peg$parseprimary();
			}

			return s0;
		}

		function peg$parseprimary(){
			var s0, s1, s2, s3, s4, s5;

			s0 = peg$parsevariant();
			if (s0 === peg$FAILED){
				s0 = peg$currPos;
				s1 = peg$parsews();
				if (s1 !== peg$FAILED){
					if (input.charCodeAt(peg$currPos) === 40){
						s2 = peg$c13;
						peg$currPos++;
					} else {
						s2 = peg$FAILED;
						if (peg$silentFails === 0){
							peg$fail(peg$c14);
						}
					}
					if (s2 !== peg$FAILED){
						s3 = peg$parseop_and();
						if (s3 !== peg$FAILED){
							if (input.charCodeAt(peg$currPos) === 41){
								s4 = peg$c15;
								peg$currPos++;
							} else {
								s4 = peg$FAILED;
								if (peg$silentFails === 0){
									peg$fail(peg$c16);
								}
							}
							if (s4 !== peg$FAILED){
								s5 = peg$parsews();
								if (s5 !== peg$FAILED){
									peg$reportedPos = s0;
									s1 = peg$c17(s1, s3, s5);
									s0 = s1;
								} else {
									peg$currPos = s0;
									s0 = peg$c3;
								}
							} else {
								peg$currPos = s0;
								s0 = peg$c3;
							}
						} else {
							peg$currPos = s0;
							s0 = peg$c3;
						}
					} else {
						peg$currPos = s0;
						s0 = peg$c3;
					}
				} else {
					peg$currPos = s0;
					s0 = peg$c3;
				}
			}

			return s0;
		}

		function peg$parsevariant(){
			var s0, s1, s2, s3;

			peg$silentFails++;
			s0 = peg$currPos;
			s1 = peg$parsews();
			if (s1 !== peg$FAILED){
				s2 = [];
				if (peg$c19.test(input.charAt(peg$currPos))){
					s3 = input.charAt(peg$currPos);
					peg$currPos++;
				} else {
					s3 = peg$FAILED;
					if (peg$silentFails === 0){
						peg$fail(peg$c20);
					}
				}
				if (s3 !== peg$FAILED){
					while (s3 !== peg$FAILED){
						s2.push(s3);
						if (peg$c19.test(input.charAt(peg$currPos))){
							s3 = input.charAt(peg$currPos);
							peg$currPos++;
						} else {
							s3 = peg$FAILED;
							if (peg$silentFails === 0){
								peg$fail(peg$c20);
							}
						}
					}
				} else {
					s2 = peg$c3;
				}
				if (s2 !== peg$FAILED){
					s3 = peg$parsews();
					if (s3 !== peg$FAILED){
						peg$reportedPos = s0;
						s1 = peg$c21(s1, s2, s3);
						s0 = s1;
					} else {
						peg$currPos = s0;
						s0 = peg$c3;
					}
				} else {
					peg$currPos = s0;
					s0 = peg$c3;
				}
			} else {
				peg$currPos = s0;
				s0 = peg$c3;
			}
			peg$silentFails--;
			if (s0 === peg$FAILED){
				s1 = peg$FAILED;
				if (peg$silentFails === 0){
					peg$fail(peg$c18);
				}
			}

			return s0;
		}


		function variantExists(variant){
			var identifier = "",
			    exists     = false,
			    i, c, v;
			for (i = 0, c = variant.length; i < c; ++i){
				identifier += variant[i];
			}
			for (i = 0, c = variants.length; i < c; ++i){
				v = variants[i];
				if (v == identifier){
					exists = true;
					break;
				}
			}
			return exists;
		}


		peg$result = peg$startRuleFunction();

		if (peg$result !== peg$FAILED && peg$currPos === input.length){
			return peg$result;
		} else {
			if (peg$result !== peg$FAILED && peg$currPos < input.length){
				peg$fail({type: "end", description: "end of input"});
			}

			throw peg$buildException(null, peg$maxFailExpected, peg$maxFailPos);
		}
	}

	return parse(input);
};
