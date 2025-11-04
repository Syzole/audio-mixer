// src/plugin.ts
import WebSocket from "ws";
import streamDeck, { LogLevel } from "@elgato/streamdeck";
import { VolumeControlAction } from "./actions/volume-control";
import { VolumeStepAction } from "./actions/volume-step";

// Optional: Enable detailed logging
streamDeck.logger.setLevel(LogLevel.TRACE);

// Connect to Stream Deck
streamDeck.connect();

// Register your actions
streamDeck.actions.registerAction(new VolumeControlAction());
streamDeck.actions.registerAction(new VolumeStepAction());

// 🔌 WebSocket setup
export let socket: WebSocket;

console.log("Connecting to NativeAudioHelper...");

interface SetVolumeMessage {
	type: "setVolume";
	app: string;
	value: number;
}

interface ToggleMuteMessage {
	type: "toggleMute";
	app: string;
}

interface GetStatusMessage {
	type: "getStatus";
	app?: string;
}

export interface dynamicAdjustVolumeMessage {
	type: "adjustVolume";
	app: string;
	direction: "up" | "down";
	amount: number;
}

interface getRunningApps{
	type: "getRunningApps";
}

type PluginToServerMessage = SetVolumeMessage | ToggleMuteMessage | GetStatusMessage | dynamicAdjustVolumeMessage | getRunningApps;

function connectWebSocket() {
	//streamDeck.logger.info("Connecting to NativeAudioHelper...");
	console.log("Connecting to NativeAudioHelper...");
	socket = new WebSocket("ws://127.0.0.1:8181");
	socket.onopen = () => {
		streamDeck.logger.info("✅ Connected to NativeAudioHelper");
		// Request the list of running applications
		sendMessage({ type: "getRunningApps" });
	};

	socket.onmessage = (event) => {
		streamDeck.logger.info("🔄 Response:", event.data);
	};

	socket.onerror = (err) => {
		//streamDeck.logger.error("❌ WebSocket error:", err);
	};

	socket.onclose = () => {
		//streamDeck.logger.warn("⚠️ WebSocket closed. Retrying in 3s...");
		setTimeout(connectWebSocket, 3000);
	};
}

// Start WebSocket connection
connectWebSocket();

export function sendMessage(message: PluginToServerMessage) {
	if (socket && socket.readyState === WebSocket.OPEN) {
		socket.send(JSON.stringify(message));
	} else {
		console.warn("WebSocket not connected");
	}
}
