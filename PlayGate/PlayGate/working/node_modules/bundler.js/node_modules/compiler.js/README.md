# compiler.js

## Tool to preprocess and compile JavaScript files

compiler.js is great and easy to use tool to produce preprocessed and minified JS files.

[![NPM](https://nodei.co/npm/compiler.js.png?downloads=true&downloadRank=true&stars=true)](https://nodei.co/npm/compiler.js/)

## Installation

```bash
$ npm install compiler.js
```

If you want to use compiler.js from terminal, you should install it globally:

```bash
$ npm install -g compiler.js
```

## API Usage Examples

```javascript
var compiler = require('compiler.js'), // load compiler.js module.
    config   = {
	    verbose:      true,                     // we want to see all logs.
	    entry:        'src/main.js',            // path to entry JavaScript file.
	    intermediate: 'intermediate/app.js',    // [optional] path to intermediate (preprocessed) JavaScript file.
	    output:       'bin/app.js',             // path ti final (binary/minified) JavaScript file.
	    basedir:      'src/',                   // base dir path for files that will be included.
	    defines:      {
		    'DEBUG': true                       // variable available at preprocessing time.
	    },
	    minify:       false                     // we do not want to minify final file.
    };

compiler.compile(config);
```
or from configuration file:
```javascript
var compiler = require('compiler.js');

compiler.compile('compilation.js');
```

## Commandline Usage Examples

Using configuration file:
```bash
$ compile path/to/configuration.json
```

Using parameters:
```bash
$ compile -v -e:src/main.js -i:intermediate/app.js -o:bin/app.js -b:src/ -d:DEBUG=true -l:true -m:false
```

Using configuration file overrided by parameters:
```bash
$ compile path/to/configuration.json -v -d:RELEASE=true -l:true -m:true
```

Commandline options:
 * `path/to/configuration.json`             - path to configuration JSON file.
 * `--verbose` or `-v`                      - determines if program should print not only errors.
 * `--entry:path` or `-e:path`              - entry file path.
 * `--intermediate:path` or `-i:path`       - intermediate file path.
 * `--output:path` or `-o:path`             - output file path.
 * `--basedir:path` or `-b:path`            - base directory path (for included files).
 * `--define:NAME=value` or `-d:NAME=value` - variable definition available at compile time.
 * `--minify:boolean` or `-m:boolean`       - determines if minify will be used.

## Configuration File Example

```json
{
  "verbose": true,
  "entry": "src/main.js",
  "intermediate": "intermediate/app.js",
  "output": "bin/app.js",
  "basedir": "src/",
  "defines": {
    "DEBUG": true
  },
  "minify": true
}
```

## Support
 * [Issues](https://github.com/PsichiX/compiler.js/issues)