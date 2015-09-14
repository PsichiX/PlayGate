var args = process.argv,
	bundler = require('./node_modules/bundler.js/index.js'),
	glob = require('./node_modules/glob/glob.js'),
	fs = require('fs-extra'),
	path = require('path'),
	target;

var config = {
	verbose: true,
	source: 'src',
	destination: {
		Debug: 'bin/Debug',
		Release: 'bin/Release'
	},
	compiler: {
		Debug: {
			minify: false,
			defines: {
				DEBUG: true
			}
		},
		Release: {
			minify: true,
			defines: {
				RELEASE: true
			}
		}
	},
	files: [
		'.include_script_assets.js @clearScriptsList ? code',
		'../assets/**/*.scene : assets/scenes/ @copyContent ? asset & scene',
		'../assets/**/*.png : assets/textures/ @copyContent ? asset & texture',
		'../assets/**/*.jpg : assets/textures/ @copyContent ? asset & texture',
		'../assets/**/*.jpeg : assets/textures/ @copyContent ? asset & texture',
		'../assets/**/*.cubemap : assets/cubemaps/ @copyContent ? asset & cubemap',
		'../assets/**/*.model : assets/models/ @copyContent ? asset & model',
		'../assets/**/*.material : assets/materials/ @copyContent ? asset & material',
		'../assets/**/*.animation : assets/animations/ @copyContent ? asset & animation',
		'../assets/**/*.mp3 : assets/audio/ @copyContent ? asset & audio',
		'../assets/**/*.ogg : assets/audio/ @copyContent ? asset & audio',
		'../assets/**/*.html : assets/html/ @copyContent ? asset & html',
		'../assets/**/*.css : assets/css/ @copyContent ? asset & css',
		'../assets/**/*.json : assets/json/ @copyContent ? asset & json',
		'../assets/**/*.txt : assets/texts/ @copyContent ? asset & text',
		'../assets/**/*.js : .include_script_assets.js @listScripts ? asset & code',
		'../templates/playgatelogo32.png : playgatelogo32.png ? template',
		'../templates/app.wrapper.html : index.html ? template',
		'../templates/app.wrapper.js : app.js @compile ? code'
	],
	variants: {
		Debug: [
			'template',
			'code',
			'asset',
			'scene',
			'texture',
			'cubemap',
			'model',
			'material',
			'animation',
			'audio',
			'html',
			'css',
			'json',
			'text'
		],
		Release: [
			'template',
			'code',
			'asset',
			'scene',
			'texture',
			'cubemap',
			'model',
			'material',
			'animation',
			'audio',
			'html',
			'css',
			'json',
			'text'
		]
	}
};

var actions = {
	clearScriptsList: function(file, config, info, bundleDirs, bundlerActions){
		var fname = bundleDirs.source + info.path;
		fs.ensureFileSync(fname);
		fs.truncateSync(fname, 0);
		config.verbose && console.log('Cleared scripts list: ' + fname);
	},
	listScripts: function(file, config, info, bundleDirs, bundlerActions){
		var src = bundleDirs.source + info.path,
			dst = bundleDirs.source + info.pathTo,
			files = glob.sync(src, null),
			content;
		if(files && files.length > 0){
			var i, c;
			for(i = 0, c = files.length; i < c; ++i){
				content = '(function(){\n../' + files[i] + '})();\n';
				fs.appendFileSync(dst, content);
				config.verbose && console.log('Script listed: ' + files[i]);
			}
		}
	},
	copyContent: function(file, config, info, bundleDirs, bundlerActions){
		var src = bundleDirs.source + info.path,
			dst = bundleDirs.destination + info.pathTo,
			files = glob.sync(src, null),
			fname;
		if(files && files.length > 0){
			var i, c;
			for(i = 0, c = files.length; i < c; ++i){
				fname = dst + path.basename(files[i]);
				fs.copySync(files[i], fname);
				config.verbose && console.log('File copied: ' + files[i] + ' -> ' + fname);
			}
		}
	}
};

if(args.length > 1){
	target = args[args.length - 1];
}

if(config && target){
	bundler.bundle(config, actions, target);
}
