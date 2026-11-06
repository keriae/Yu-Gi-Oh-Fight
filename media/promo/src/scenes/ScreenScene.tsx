// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import {
  AbsoluteFill,
  Easing,
  Interactive,
  interpolate,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';
import { Video } from '@remotion/media';
import { measureText } from '@remotion/layout-utils';
import { chineseFont, font, latinFont, palette } from '../style';
import { usePromoFonts } from '../fonts';
import { chapterText, localizedRecording, usePromoLanguage } from '../language';

export type ScreenSceneProps = {
  src: string;
  start?: number;
  chapter: string;
  english: string;
  chinese: string;
  zoom?: number;
  top?: number;
  left?: number;
};

export const ScreenScene: React.FC<ScreenSceneProps> = ({
  src,
  start = 0,
  chapter,
  english,
  chinese,
  zoom = 1,
  top = -60,
  left = 0,
}) => {
  const frame = useCurrentFrame();
  const { fps, durationInFrames } = useVideoConfig();
  const language = usePromoLanguage();
  const fontsLoaded = usePromoFonts();
  if (!fontsLoaded) return null;
  const recording = localizedRecording(src, start, language);
  const caption = language === 'en' ? english : chinese;
  const captionFont = language === 'en' ? latinFont : chineseFont;
  const chapterLabel = chapterText(chapter, language);
  const chapterLetterSpacing = language === 'en' ? 2.5 : 1.5;
  const chapterWidth = measureText({
    text: chapterLabel,
    fontFamily: captionFont,
    fontSize: 20,
    fontWeight: '600',
    letterSpacing: `${chapterLetterSpacing}px`,
    validateFontIsLoaded: true,
  }).width;
  const chapterScale = Math.min(1, 168 / chapterWidth);
  const captionWidth = measureText({
    text: caption,
    fontFamily: captionFont,
    fontSize: 44,
    fontWeight: '600',
    letterSpacing: '.3px',
    validateFontIsLoaded: true,
  }).width;
  const captionScale = Math.min(1, 1516 / captionWidth);
  return (
    <AbsoluteFill
      style={{
        backgroundColor: palette.background,
        fontFamily: font,
        fontSynthesis: 'none',
      }}
    >
      <Video
        src={staticFile('footage/' + recording.src)}
        trimBefore={recording.start * fps}
        style={{
          position: 'absolute',
          width: 1920 * zoom,
          height: 1200 * zoom,
          left,
          top: language === 'en' && top === -60 ? 0 : top,
        }}
        objectFit="cover"
        volume={(f) =>
          interpolate(
            f,
            [0, 8, durationInFrames - 10, durationInFrames - 1],
            [0.1, 0.65, 0.65, 0.1],
            { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
          )
        }
      />
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          height: 124,
          left: 0,
          right: 0,
          background:
            'linear-gradient(0deg, #030b12fa 0%, #030b12e8 54%, #030b1200 100%)',
        }}
      />
      <div
        data-promo-caption={language}
        style={{
          position: 'absolute',
          left: 94,
          right: 94,
          bottom: 36,
          display: 'flex',
          alignItems: 'center',
          gap: 28,
          whiteSpace: 'nowrap',
          opacity: interpolate(frame, [6, 20], [0, 1], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          }),
          translate: interpolate(frame, [6, 20], ['0px 9px', '0px 0px'], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
            easing: Easing.bezier(0.2, 0.8, 0.2, 1),
          }),
        }}
      >
        <Interactive.Div
          name="Chapter"
          style={{
            fontFamily: captionFont,
            fontWeight: 600,
            color: palette.cyan,
            fontSize: 20 * chapterScale,
            letterSpacing: chapterLetterSpacing * chapterScale,
            width: 188,
            boxSizing: 'border-box',
            textAlign: 'center',
            padding: '12px 10px',
            border: '1px solid #b49a6159',
            background: '#050e16d9',
            flexShrink: 0,
          }}
        >
          {chapterLabel}
        </Interactive.Div>
        <Interactive.Div
          name="Caption"
          style={{
            fontFamily: captionFont,
            fontSize: 44 * captionScale,
            fontWeight: 600,
            lineHeight: 1.35,
            color: '#efe6ce',
            letterSpacing: 0.3 * captionScale,
            flexShrink: 0,
            textShadow: '0 2px 8px #000',
          }}
        >
          {caption}
        </Interactive.Div>
      </div>
    </AbsoluteFill>
  );
};
