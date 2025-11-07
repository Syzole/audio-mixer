import streamDeck, { action, SingletonAction, WillAppearEvent } from "@elgato/streamdeck";
import { sendMessage, socket } from "../plugin";

@action({ UUID: "com.syzole.audio-mixer.volume-controller" })
export class VolumeControlAction extends SingletonAction {
  override async onWillAppear(ev: WillAppearEvent): Promise<void> {
    let response = await sendMessage({ type: "getRunningApps" });
    streamDeck.settings.setGlobalSettings({ runningApps: response } as any);
  }
}