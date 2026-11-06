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
import { PromoLanguageContext, type PromoLanguage } from './language';

const scenes = [
  ['Opening', Opening, 180],
  ['Decks', Decks, 270],
  ['Editor', Editor, 270],
  ['Academy', Academy, 270],
  ['Practice', Practice, 270],
  ['AiProfiles', AiProfiles, 240],
  ['AiBattle', AiBattle, 240],
  ['Duel', Duel, 360],
  ['Menus', Menus, 270],
  ['Replays', Replays, 240],
  ['Closing', Closing, 180],
] as const;

const localizedScenes = (language: PromoLanguage) =>
  scenes.map(([name, Scene, durationInFrames]) => ({
    id: `${language.toUpperCase()}-${name}`,
    component: () => (
      <PromoLanguageContext.Provider value={language}>
        <Scene />
      </PromoLanguageContext.Provider>
    ),
    durationInFrames,
  }));
const englishScenes = localizedScenes('en');
const chineseScenes = localizedScenes('zh');

export const RemotionRoot = () => (
  <>
    <Composition
      id="LaunchTrailerEN"
      component={LaunchTrailer}
      defaultProps={{ language: 'en' }}
      durationInFrames={2700}
      fps={30}
      width={1920}
      height={1080}
    />
    <Composition
      id="LaunchTrailerZH"
      component={LaunchTrailer}
      defaultProps={{ language: 'zh' }}
      durationInFrames={2700}
      fps={30}
      width={1920}
      height={1080}
    />
    <Folder name="English-Scenes">
      {englishScenes.map((scene) => (
        <Composition
          key={scene.id}
          {...scene}
          fps={30}
          width={1920}
          height={1080}
        />
      ))}
    </Folder>
    <Folder name="Chinese-Scenes">
      {chineseScenes.map((scene) => (
        <Composition
          key={scene.id}
          {...scene}
          fps={30}
          width={1920}
          height={1080}
        />
      ))}
    </Folder>
  </>
);
