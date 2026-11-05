// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { Composition, Folder } from 'remotion';
import { LaunchTrailer } from './Composition';
import { Opening } from './scenes/Opening';
import { Decks } from './scenes/Decks';
import { Editor } from './scenes/Editor';
import { Academy } from './scenes/Academy';
import { Practice } from './scenes/Practice';
import { AiProfiles } from './scenes/AiProfiles';
import { AiBattle } from './scenes/AiBattle';
import { Duel } from './scenes/Duel';
import { Menus } from './scenes/Menus';
import { Replays } from './scenes/Replays';
import { Closing } from './scenes/Closing';
export const RemotionRoot = () => (
  <>
    <Composition
      id="LaunchTrailer"
      component={LaunchTrailer}
      durationInFrames={2700}
      fps={30}
      width={1920}
      height={1080}
    />
    <Folder name="Scenes">
      <Composition
        id="Opening"
        component={Opening}
        durationInFrames={180}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Decks"
        component={Decks}
        durationInFrames={270}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Editor"
        component={Editor}
        durationInFrames={270}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Academy"
        component={Academy}
        durationInFrames={270}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Practice"
        component={Practice}
        durationInFrames={270}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="AiProfiles"
        component={AiProfiles}
        durationInFrames={240}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="AiBattle"
        component={AiBattle}
        durationInFrames={240}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Duel"
        component={Duel}
        durationInFrames={360}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Menus"
        component={Menus}
        durationInFrames={270}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Replays"
        component={Replays}
        durationInFrames={240}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Closing"
        component={Closing}
        durationInFrames={180}
        fps={30}
        width={1920}
        height={1080}
      />
    </Folder>
  </>
);
