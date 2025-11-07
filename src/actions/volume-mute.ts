import streamDeck, { action, SingletonAction, WillAppearEvent } from "@elgato/streamdeck";
import { sendMessage, socket } from "../plugin";

@action({ UUID: "com.syzole.audio-mixer.volume-mute" })
export class VolumeMuteAction extends SingletonAction{
    
}