(function(exports){

	exports.pgSendMessage = function(message, data){
		
		var msg = new MessageEvent(message, {
			view: window,
			bubbles: false,
			cancelable: false,
			data: JSON.stringify(data)
		});
		document.dispatchEvent(msg);

	};

	exports.pgEditorClose = function(data){

		exports.pgSendMessage('pgEditorClose', data);

	};

	exports.pgEditorApplyValue = function(data){

		exports.pgSendMessage('pgEditorApplyValue', data);

	};

	exports.pgEditorRequestValue = function(){

		exports.pgSendMessage('pgEditorRequestValue', null);

	};

	exports.pgEditorApplyWindowSize = function(width, height){

		exports.pgSendMessage('pgEditorApplyWindowSize', {
			width: width,
			height: height
		});

	};

})(window);
