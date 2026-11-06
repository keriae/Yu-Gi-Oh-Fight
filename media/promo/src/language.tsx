// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { createContext, useContext } from 'react';

export type PromoLanguage = 'en' | 'zh';
export const PromoLanguageContext = createContext<PromoLanguage>('en');
export const usePromoLanguage = () => useContext(PromoLanguageContext);

export const menuThemeRecording = (language: PromoLanguage) =>
  language === 'en'
    ? { src: 'home-en.mp4', start: 2 }
    : { src: 'features-zh.mp4', start: 18 };

const chineseChapters: Record<string, string> = {
  '01 / BEGIN': '01 / 开始',
  '02 / BUILD': '02 / 构筑',
  '03 / LEARN': '03 / 学习',
  '04 / CHALLENGE': '04 / 对战',
  '05 / DUEL': '05 / 决斗',
  '06 / REVIEW': '06 / 回放',
};

export const chapterText = (chapter: string, language: PromoLanguage) =>
  language === 'zh' ? chineseChapters[chapter] : chapter;

// Each localized capture is a separate recording of the real game window.
const englishRecordings: Record<string, string> = {
  'features-zh.mp4:39': 'decks-en.mp4',
  'features-zh.mp4:83': 'editor-en.mp4',
  'academy-zh.mp4:5': 'academy-en.mp4',
  'tutorial-zh.mp4:2': 'tutorial-en.mp4',
  'tutorial-step-zh.mp4:2': 'tutorial-step-en.mp4',
  'player-vs-ai-zh.mp4:2': 'player-vs-ai-en.mp4',
  'ai-vs-ai-zh.mp4:2': 'ai-vs-ai-en.mp4',
  'full-replay-zh.mp4:36': 'duel-en.mp4',
  'menus-zh.mp4:18': 'menu-settings-en.mp4',
  'menus-zh.mp4:48': 'menu-log-en.mp4',
  'menus-zh.mp4:62': 'menu-card-en.mp4',
  'full-replay-zh.mp4:12': 'replay-library-en.mp4',
  'full-replay-zh.mp4:92': 'replay-playback-en.mp4',
};

export const localizedRecording = (
  src: string,
  start: number,
  language: PromoLanguage,
) => {
  if (src === 'home-en.mp4') return { src: `home-${language}.mp4`, start };
  if (language === 'zh' && src === 'full-replay-zh.mp4' && start === 12) {
    return { src: 'replay-library-zh.mp4', start: 2 };
  }
  if (language === 'en') {
    const localized = englishRecordings[`${src}:${start}`];
    if (localized) return { src: localized, start: 2 };
  }
  return { src, start };
};
