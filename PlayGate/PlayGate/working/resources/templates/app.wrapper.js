(function(){

	var module = {
		exports: {}
	};

	// #include_once "../lib/playcanvas.js"
	// #include_once ".include_script_assets.js"
	// #include_once "main.js"

	if(module.exports.main && module.exports.main instanceof Function){
		window.onload = module.exports.main;
		if(module.exports.exit && module.exports.exit instanceof Function){
			window.onexit = module.exports.exit;
		}
	} else {
		console.error('Application entry point not found in module.exports!');
	}

})();
