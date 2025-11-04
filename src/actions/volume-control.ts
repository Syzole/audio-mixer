import streamDeck, { action, DialDownEvent, DialRotateEvent, JsonObject, SingletonAction, TouchTapEvent, WillAppearEvent } from "@elgato/streamdeck";
import { sendMessage, socket } from "../plugin";
import { send, title } from "process";

const APP_NAME = "Chrome";

let currentVolume = 100; // fallback default

@action({ UUID: "com.syzole.audio-mixer.volume" })
export class VolumeControlAction extends SingletonAction {
	override async onWillAppear(ev: WillAppearEvent): Promise<void> {
		sendMessage({ type: "getStatus", app: APP_NAME });

		socket.addEventListener(
			"message",
			(event) => {
				try {
					const data = JSON.parse(event.data.toString());
					if (data.app === APP_NAME && typeof data.volume === "number") {
						currentVolume = data.volume;
						if (ev.action.isDial()) {
							ev.action.setFeedback({
								indicator: {
									value: data.volume,
								},
								value: {
									value: `${data.volume}%`,
								},
								title: {
									value: `${APP_NAME}`,
								},
							});
						}
						//streamDeck.logger.info(`📡 Synced volume: ${currentVolume}`);
					}
				} catch (err) {
					streamDeck.logger.info("Invalid status response:", err);
				}
			},
			{ once: true }
		);
	}

	//This is for the regular button

	//These 3 are for the dial or stream deck plus
	override async onDialDown(ev: DialDownEvent) {
		sendMessage({ type: "toggleMute", app: APP_NAME });
	}

	override async onTouchTap(ev: TouchTapEvent) {
		//streamDeck.logger.info("🔇 Toggling mute for", APP_NAME);
		sendMessage({ type: "toggleMute", app: APP_NAME });
	}
	
	override async onDialRotate(ev: DialRotateEvent) {
		const direction = ev.payload.ticks > 0 ? "up" : "down";
		const step = ev.payload.ticks > 0 ? 5: -5;
		sendMessage({ type: "adjustVolume", app: APP_NAME, direction, amount: Math.abs(ev.payload.ticks) });

		socket.addEventListener(
			"message",
			(event) => {
				try {
					const data = JSON.parse(event.data.toString());
					data.volume = Math.round(data.volume); // Ensure volume is an integer
					streamDeck.logger.info("🔄 Response:", data);
					streamDeck.logger.info("🔄 Volume:", data.volume);
					if (typeof data.volume === "number") {
						if (ev.action.isDial()) {
							ev.action.setFeedback({
								indicator: {
									value: data.volume,
								},
								value: {
									value: `${data.volume}%`,
								},
							});
						}
						//streamDeck.logger.info(`📡 Synced volume: ${currentVolume}`);
					}
				} catch (err) {
					streamDeck.logger.info("Invalid status response:", err);
				}
			},
			{ once: true }
		);
	}
}

//helper functions
function createVolumeBarImage(volume: number): string {
	const canvas = document.createElement("canvas");
	const ctx = canvas.getContext("2d")!;
	canvas.width = 144;
	canvas.height = 144;

	// Background
	ctx.fillStyle = "#000";
	ctx.fillRect(0, 0, canvas.width, canvas.height);

	// Border
	ctx.strokeStyle = "#555";
	ctx.lineWidth = 4;
	ctx.strokeRect(10, 114, 124, 20);

	// Filled bar
	const barWidth = (volume / 100) * 120;
	ctx.fillStyle = "#00ff88";
	ctx.fillRect(12, 116, barWidth, 16);

	// Volume text
	ctx.fillStyle = "#ffffff";
	ctx.font = "bold 24px sans-serif";
	ctx.textAlign = "center";
	ctx.fillText(`${volume}%`, canvas.width / 2, 60);

	return canvas.toDataURL("image/png");
}
