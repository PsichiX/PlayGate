(function(exports){

	var PlayGateApplication = exports.PlayGateApplication;

	function main(){

		exports.pgApp = new PlayGateApplication('application-canvas');

	}

	function exit(){

		if(exports.pgApp){
			exports.pgApp.destroy();
			exports.pgApp = null;
		}

	}

	exports.main = main;
	exports.exit = exit;

})(module.exports);
