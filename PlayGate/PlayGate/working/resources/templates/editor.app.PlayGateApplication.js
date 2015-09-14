(function(exports){

	function PlayGateApplication(canvasId){

		var canvas = document.getElementById(canvasId),
			app = this._app = new pc.Application(canvas, {});

		app.start();
		app.setCanvasFillMode(pc.FILLMODE_FILL_WINDOW);
		app.setCanvasResolution(pc.RESOLUTION_AUTO);
		
		var camera = this._camera = new pc.Entity();
		camera.addComponent('camera', {
			clearColor: new pc.Color(0.1, 0.2, 0.3)
		});
		app.root.addChild(camera);

		app.on("update", function(deltaTime){
		});

		window.addEventListener('resize', function(){
			app.resizeCanvas(canvas.width, canvas.height);
		});

	}

	PlayGateApplication.prototype = Object.create(null);

	PlayGateApplication.prototype._app = null;
	PlayGateApplication.prototype._camera = null;

	PlayGateApplication.prototype.destroy = function(){

		var app = this._app,
			camera = this._camera;

		if(app){
			if(camera){
				app.root.removeChild(camera);
			}
		}

		this._app = null;
		this._camera = null;

	};

	exports.PlayGateApplication = PlayGateApplication;

})(module.exports);
