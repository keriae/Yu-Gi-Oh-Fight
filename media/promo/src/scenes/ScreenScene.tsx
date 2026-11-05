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
import { font, palette } from '../style';

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
  return (
    <AbsoluteFill
      style={{ backgroundColor: palette.background, fontFamily: font }}
    >
      <Video
        src={staticFile(`footage/${src}`)}
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
      <Interactive.Div
        name="Chapter"
        style={{
          position: 'absolute',
          bottom: 153,
          left: 94,
          zIndex: 2,
          color: palette.cyan,
          fontSize: 20,
          letterSpacing: 3,
          opacity: interpolate(frame, [0, 15], [0, 1], {
            extrapolateRight: 'clamp',
          }),
        }}
      >
        {chapter}
      </Interactive.Div>
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
      {reveal && (
        <div
          style={{
            position: 'absolute',
            left: 94,
            top: 350,
            opacity: interpolate(frame, [0, 18, 75, 115], [0, 1, 1, 0], {
              extrapolateRight: 'clamp',
            }),
            translate: interpolate(frame, [0, 24], ['0px 18px', '0px 0px'], {
              extrapolateRight: 'clamp',
              easing: Easing.bezier(0.2, 0.8, 0.2, 1),
            }),
          }}
        >
          <div
            style={{
              fontSize: 104,
              color: palette.white,
              fontWeight: 650,
              letterSpacing: -3,
            }}
          >
            Yu-Gi-Oh! <span style={{ color: palette.gold }}>Fight</span>
          </div>
          <div
            style={{
              fontSize: 36,
              color: palette.cyan,
              marginTop: 18,
              letterSpacing: 2,
            }}
          >
            YOUR DECK. YOUR DUEL.
          </div>
        </div>
      )}
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          height: 225,
          left: 0,
          right: 0,
          background:
            'linear-gradient(0deg, #030b12f5 0%, #030b12d9 48%, #030b1200 100%)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          left: 94,
          right: 94,
          bottom: 34,
          opacity: interpolate(frame, [6, 20], [0, 1], {
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
            fontSize: 48,
            color: palette.white,
            fontWeight: 600,
            letterSpacing: -0.8,
          }}
        >
          {english}
        </Interactive.Div>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 24,
            marginTop: 8,
          }}
        >
          <div style={{ width: 40, height: 2, background: palette.gold }} />
          <Interactive.Div
            name="Chinese caption"
            style={{ fontSize: 28, color: palette.gold, letterSpacing: 1 }}
          >
            {chinese}
          </Interactive.Div>
          {detail && (
            <div
              style={{ marginLeft: 'auto', fontSize: 22, color: palette.muted }}
            >
              {detail}
            </div>
          )}
        </div>
      </div>
    </AbsoluteFill>
  );
};
