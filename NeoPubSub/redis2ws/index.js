const redis = require("redis")
const WebSocket = require('ws');

const subscriber = redis.createClient()
const wss = new WebSocket.Server({ port: 8000 });

subscriber.on("message", function(channel, message) {
	wss.clients.forEach(function each(client) {
		if (client.readyState === WebSocket.OPEN) {
			client.send(message);
		}
	});
});

subscriber.subscribe("events");
