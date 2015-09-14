(function(exports){

	function main(){
		
		// Create a PlayCanvas application
		var canvas = document.getElementById("application-canvas");
		var app = new pc.Application(canvas, {});

		app.start();

		// Fill the available space at full resolution
		app.setCanvasFillMode(pc.FILLMODE_FILL_WINDOW);
		app.setCanvasResolution(pc.RESOLUTION_AUTO);

		// Create box entity
		var cube = new pc.Entity();
		cube.addComponent('model', {
			type: "box"
		});

		// Create camera entity
		var camera = new pc.Entity();
		camera.addComponent('camera', {
			clearColor: new pc.Color(0.1, 0.2, 0.3)
		});

		// Create directional light entity
		var light = new pc.Entity();
		light.addComponent('light');

		// Add to hierarchy
		app.root.addChild(cube);
		app.root.addChild(camera);
		app.root.addChild(light);

		// Set up initial positions and orientations
		camera.setPosition(0, 0, 3);
		light.setEulerAngles(45, 0, 0);

		// Register an update event
		app.on("update", function (deltaTime) {
		cube.rotate(10 * deltaTime, 20 * deltaTime, 30 * deltaTime);
		});

		window.addEventListener('resize', function () {
			app.resizeCanvas(canvas.width, canvas.height);
		});

	}

	exports.main = main;

})(module.exports);
