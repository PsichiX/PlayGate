(function(global){

	//  #ifdef DEBUG
	global.mode = "debug";
	//  #else
	//      #ifdef RELEASE
	global.mode = "release";
	//      #else
	global.mode = "undefined";
	//      #endif
	//  #endif

})(window);
