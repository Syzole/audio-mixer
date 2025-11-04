import streamDeck, { action,  JsonObject,  KeyDownEvent,  SingletonAction, WillAppearEvent } from "@elgato/streamdeck";
import { dynamicAdjustVolumeMessage, sendMessage, socket } from "../plugin";
import { send } from "process";

const APP_NAME = "Chrome";

let currentVolume = 100; // fallback default

@action({ UUID: "com.syzole.audio-mixer.volumestep" })
export class VolumeStepAction extends SingletonAction {
  override async onWillAppear(ev: WillAppearEvent): Promise<void> {
    sendMessage({ type: "getStatus", app: APP_NAME });

    socket.addEventListener(
      "message",
      (event) => {
        try {
          const data = JSON.parse(event.data.toString());
          if (data.app === APP_NAME && typeof data.volume === "number") {
            currentVolume = data.volume;
            if (ev.action.isKey()){
                ev.action.setTitle(
                    `${APP_NAME}\n${data.volume}%`
                );
            }
          }
        } catch (err) {
          streamDeck.logger.info("Invalid volume response:", err);
        }
      },
      { once: true }
    );
  }

  override async onKeyDown(ev: KeyDownEvent<JsonObject>) {
    const settings = await ev.action.getSettings() || {};
    const step = settings.volumeStep || 5;
    const direction = settings.direction as "up" | "down" || "up";

    streamDeck.logger.info(`🔊 Adjusting volume ${direction} by ${step}% for ${APP_NAME}`);

    const objFormed = { type: "adjustVolume", app: APP_NAME, direction, amount: step } as dynamicAdjustVolumeMessage

    streamDeck.logger.info("Sending message:", objFormed);

    sendMessage(objFormed);

    socket.addEventListener(
                "message",
                (event) => {
                    try {
                        const data = JSON.parse(event.data.toString());
                        data.volume = Math.round(data.volume); // Ensure volume is an integer
                        streamDeck.logger.info("🔄 Response:", data);
                        streamDeck.logger.info("🔄 Volume:", data.volume);
                        if (typeof data.volume === "number") {
                            if (ev.action.isKey()) {
                                ev.action.setTitle(`${APP_NAME}\n${data.volume}%`);
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

}
