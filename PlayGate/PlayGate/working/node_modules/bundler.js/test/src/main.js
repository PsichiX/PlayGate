// #include_once "bundle.js"
// #include_once "bundle.js"

(function(global){

	function main(){

		var mode;
		//  #ifdef DEBUG
		mode = "debug";
		//  #else
		//      #ifdef RELEASE
		mode = "release";
		//      #else
		mode = "undefined";
		//      #endif
		//  #endif

		console.log('mode ' + mode);

	}

	main();

})(window);
