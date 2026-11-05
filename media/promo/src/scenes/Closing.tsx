// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import {
  AbsoluteFill,
  Interactive,
  interpolate,
  staticFile,
  useCurrentFrame,
} from 'remotion';
import { Audio } from '@remotion/media';
import { font, palette } from '../style';
export const Closing = () => {
  const frame = useCurrentFrame();
  return (
    <AbsoluteFill
      style={{
        backgroundColor: palette.background,
        fontFamily: font,
        color: palette.white,
        justifyContent: 'center',
        alignItems: 'center',
      }}
    >
      <Audio
        src={staticFile('footage/features-zh.mp4')}
        trimBefore={18 * 30}
        volume={(f) =>
          interpolate(f, [0, 15, 120, 179], [0, 0.4, 0.4, 0], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          })
        }
      />
      <div
        style={{
          position: 'absolute',
          inset: 50,
          border: '1px solid #d2bb8438',
        }}
      />
      <div
        style={{
          position: 'absolute',
          width: 1100,
          height: 1,
          top: 350,
          background:
            'linear-gradient(90deg,transparent,#79d8e6b0,transparent)',
        }}
      />
      <Interactive.Div
        name="Closing title"
        style={{
          fontSize: 110,
          fontWeight: 650,
          letterSpacing: -3,
          opacity: interpolate(frame, [0, 15], [0, 1], {
            extrapolateRight: 'clamp',
          }),
        }}
      >
        Yu-Gi-Oh! <span style={{ color: palette.gold }}>Fight</span>
      </Interactive.Div>
      <Interactive.Div
        name="Closing tagline"
        style={{
          fontSize: 36,
          letterSpacing: 3,
          color: palette.cyan,
          marginTop: 32,
        }}
      >
        BUILD. LEARN. DUEL. REPLAY.
      </Interactive.Div>
      <Interactive.Div
        name="Project link"
        style={{ fontSize: 34, color: palette.gold, marginTop: 60 }}
      >
        github.com/keriae
      </Interactive.Div>
      <div style={{ fontSize: 23, color: palette.muted, marginTop: 25 }}>
        Complete duel recording included · 完整对战录屏见项目文档
      </div>
      <div
        style={{
          position: 'absolute',
          bottom: 82,
          textAlign: 'center',
          color: palette.muted,
        }}
      >
        <div style={{ fontSize: 20 }}>
          Unofficial community project. Not affiliated with or endorsed by
          Konami.
        </div>
        <div style={{ fontSize: 18, marginTop: 10 }}>
          非官方爱好者项目，与科乐美无关联，未获其背书。
        </div>
      </div>
    </AbsoluteFill>
  );
};
