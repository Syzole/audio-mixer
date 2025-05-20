import streamDeck, { action, DialDownEvent, DialRotateEvent, SingletonAction, WillAppearEvent } from "@elgato/streamdeck";
import { sendMessage, socket } from "../plugin";

const APP_NAME = "Spotify";

let currentVolume = 50; // fallback default

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
									value: currentVolume,
								},
							});
						}
						streamDeck.logger.info(`📡 Synced volume: ${currentVolume}`);
					}
				} catch (err) {
					streamDeck.logger.info("Invalid status response:", err);
				}
			},
			{ once: true }
		);
	}

	override async onDialDown(ev: DialDownEvent) {
		sendMessage({ type: "toggleMute", app: APP_NAME });
	}

	override async onDialRotate(ev: DialRotateEvent) {
		const direction = ev.payload.ticks > 0 ? "up" : "down";
		sendMessage({ type: "adjustVolume", app: APP_NAME, direction });
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
