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
import { TitleLockup } from '../TitleLockup';

export type ScreenSceneProps = {
  src: string;
  start?: number;
  chapter: string;
  english: string;
  chinese: string;
  detail?: string;
  reveal?: boolean;
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
  detail,
  reveal = false,
  zoom = 1,
  top = -60,
  left = 0,
}) => {
  const frame = useCurrentFrame();
  const { fps, durationInFrames } = useVideoConfig();
  const fontsLoaded = usePromoFonts();
  if (!fontsLoaded) return null;
  const englishWidth = measureText({
    text: english,
    fontFamily: latinFont,
    fontSize: 44,
    fontWeight: '600',
    letterSpacing: '.3px',
    validateFontIsLoaded: true,
  }).width;
  const chineseWidth = measureText({
    text: chinese,
    fontFamily: chineseFont,
    fontSize: 30,
    fontWeight: '600',
    letterSpacing: '.6px',
    validateFontIsLoaded: true,
  }).width;
  const captionScale = Math.min(1, (1732 - 28) / (englishWidth + chineseWidth));
  return (
    <AbsoluteFill
      style={{
        backgroundColor: palette.background,
        fontFamily: font,
        fontSynthesis: 'none',
      }}
    >
      <Video
        src={staticFile('footage/' + src)}
        trimBefore={start * fps}
        style={{
          position: 'absolute',
          width: 1920 * zoom,
          height: 1200 * zoom,
          left,
          top,
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
      {reveal && (
        <AbsoluteFill
          style={{
            backgroundColor: palette.background,
            opacity: interpolate(
              frame,
              [0, 12, 75, 120],
              [0.62, 0.45, 0.45, 0],
              { extrapolateRight: 'clamp' },
            ),
          }}
        />
      )}
      <div
        style={{
          position: 'absolute',
          bottom: 112,
          left: 94,
          zIndex: 2,
          display: 'flex',
          alignItems: 'center',
          gap: 24,
          opacity: interpolate(frame, [0, 15], [0, 1], {
            extrapolateRight: 'clamp',
          }),
        }}
      >
        <Interactive.Div
          name="Chapter"
          style={{
            fontFamily: latinFont,
            fontWeight: 600,
            color: palette.cyan,
            fontSize: 20,
            letterSpacing: 2.5,
            padding: '12px 18px',
            border: '1px solid #b49a6159',
            background: '#050e16d9',
          }}
        >
          {chapter}
        </Interactive.Div>
        {detail && (
          <div
            style={{
              fontFamily: latinFont,
              fontWeight: 600,
              fontSize: 20,
              color: '#e5dbc3',
              textShadow: '0 2px 8px #000',
            }}
          >
            {detail}
          </div>
        )}
      </div>
      {reveal && (
        <div
          style={{
            position: 'absolute',
            left: 94,
            top: 342,
            opacity: interpolate(frame, [0, 18, 75, 115], [0, 1, 1, 0], {
              extrapolateRight: 'clamp',
            }),
            translate: interpolate(frame, [0, 24], ['0px 18px', '0px 0px'], {
              extrapolateRight: 'clamp',
              easing: Easing.bezier(0.2, 0.8, 0.2, 1),
            }),
          }}
        >
          <TitleLockup size={110} />
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 24,
              marginTop: 14,
            }}
          >
            <div style={{ width: 60, height: 1, background: palette.gold }} />
            <div
              style={{
                fontFamily: latinFont,
                fontWeight: 600,
                fontSize: 32,
                color: palette.cyan,
                letterSpacing: 3,
              }}
            >
              YOUR DECK. YOUR DUEL.
            </div>
          </div>
        </div>
      )}
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          height: 148,
          left: 0,
          right: 0,
          background:
            'linear-gradient(0deg, #030b12fa 0%, #030b12e8 54%, #030b1200 100%)',
        }}
      />
      <div
        data-promo-caption="bilingual"
        style={{
          position: 'absolute',
          left: 94,
          right: 94,
          bottom: 36,
          display: 'flex',
          alignItems: 'baseline',
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
          name="English caption"
          style={{
            fontFamily: latinFont,
            fontSize: 44 * captionScale,
            fontWeight: 600,
            lineHeight: 1.35,
            color: '#efe6ce',
            letterSpacing: 0.3 * captionScale,
            flexShrink: 0,
            textShadow: '0 2px 8px #000',
          }}
        >
          {english}
        </Interactive.Div>
        <div
          aria-hidden
          style={{
            alignSelf: 'center',
            width: 1,
            height: 28,
            margin: '0 13.5px',
            background: '#d2bb8470',
            flexShrink: 0,
          }}
        />
        <Interactive.Div
          name="Chinese caption"
          style={{
            fontFamily: chineseFont,
            fontSize: 30 * captionScale,
            fontWeight: 600,
            lineHeight: 1.35,
            color: palette.gold,
            letterSpacing: 0.6 * captionScale,
            flexShrink: 0,
            textShadow: '0 2px 8px #000',
          }}
        >
          {chinese}
        </Interactive.Div>
      </div>
    </AbsoluteFill>
  );
};
