var fs      = require('fs'),
    bundler = require('../index.js'),
    list    = [],
    actions = {
	    flushAssetsList: function(file, config, info, bundleDirs, bundlerActions){
		    fs.writeFileSync(bundleDirs.destination + info.pathTo, JSON.stringify({assets: list}));
		    list = [];
		    console.log('FLUSH ASSETS LIST: ' + bundleDirs.destination + info.pathTo);
	    },
	    listAsset: function(file, config, info, bundleDirs, bundlerActions){
		    list.push(info);
	    },
	    something: function(file, config, info, bundleDirs, bundlerActions){
		    console.log('DO SOMETHING');
	    }
    };

bundler.bundle('bundle.json', actions, 'debug');
