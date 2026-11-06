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
import footage from '../../footage.json';

export type ScreenSceneProps = {
  src: string;
  start?: number;
  chapter: string;
  english: string;
  chinese: string;
};

export const ScreenScene: React.FC<ScreenSceneProps> = ({
  src,
  start = 0,
  chapter,
  english,
  chinese,
}) => {
  const frame = useCurrentFrame();
  const { fps, durationInFrames } = useVideoConfig();
  const language = usePromoLanguage();
  const fontsLoaded = usePromoFonts();
  if (!fontsLoaded) return null;
  const recording = localizedRecording(src, start, language);
  const capture = footage.files.find((file) => file.file === recording.src);
  const sourceWidth = capture?.canvas_pixels[0] ?? 1920;
  const sourceHeight = capture?.canvas_pixels[1] ?? 1200;
  // Only discard recorded black padding identified in the capture manifest.
  // Fit the entire game page above the separate caption footer.
  const contentHeight =
    capture && 'content_height_pixels' in capture
      ? (capture.content_height_pixels ?? sourceHeight)
      : sourceHeight;
  const scale = Math.min(1840 / sourceWidth, 924 / contentHeight);
  const pageWidth = sourceWidth * scale;
  const pageHeight = contentHeight * scale;
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
      <div
        data-promo-page={language}
        style={{
          position: 'absolute',
          width: pageWidth,
          height: pageHeight,
          left: (1920 - pageWidth) / 2,
          top: 24 + (924 - pageHeight) / 2,
          overflow: 'hidden',
        }}
      >
        <Video
          src={staticFile('footage/' + recording.src)}
          trimBefore={recording.start * fps}
          style={{
            position: 'absolute',
            width: pageWidth,
            height: sourceHeight * scale,
            left: 0,
            top: 0,
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
      </div>
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          height: 116,
          left: 0,
          right: 0,
          backgroundColor: palette.background,
        }}
      />
      <div
        data-promo-caption={language}
        style={{
          position: 'absolute',
          left: 94,
          right: 94,
          bottom: 32,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
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
