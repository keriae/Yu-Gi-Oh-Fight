// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import {
  AbsoluteFill,
  Interactive,
  interpolate,
  staticFile,
  useCurrentFrame,
} from 'remotion';
import { Audio } from '@remotion/media';
import { chineseFont, font, latinFont, palette } from '../style';
import { usePromoFonts } from '../fonts';
import { TitleLockup } from '../TitleLockup';
export const Closing = () => {
  const frame = useCurrentFrame();
  const fontsLoaded = usePromoFonts();
  if (!fontsLoaded) return null;
  return (
    <AbsoluteFill
      style={{
        backgroundColor: palette.background,
        fontFamily: font,
        fontSynthesis: 'none',
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
          textAlign: 'center',
          opacity: interpolate(frame, [0, 15], [0, 1], {
            extrapolateRight: 'clamp',
          }),
        }}
      >
        <TitleLockup size={118} />
      </Interactive.Div>
      <Interactive.Div
        name="Closing tagline"
        style={{
          fontFamily: latinFont,
          fontWeight: 600,
          fontSize: 32,
          letterSpacing: 3,
          color: palette.cyan,
          marginTop: 32,
        }}
      >
        BUILD. LEARN. DUEL. REPLAY.
      </Interactive.Div>
      <Interactive.Div
        name="Project link"
        style={{
          fontFamily: latinFont,
          fontSize: 30,
          fontWeight: 600,
          color: palette.gold,
          marginTop: 60,
        }}
      >
        github.com/keriae
      </Interactive.Div>
      <div
        style={{
          fontFamily: chineseFont,
          fontSize: 25,
          fontWeight: 600,
          color: palette.muted,
          marginTop: 25,
          whiteSpace: 'nowrap',
        }}
      >
        Complete duel recording included · 完整对战录屏见项目文档
      </div>
      <div
        style={{
          position: 'absolute',
          bottom: 82,
          display: 'flex',
          alignItems: 'baseline',
          justifyContent: 'center',
          gap: 22,
          whiteSpace: 'nowrap',
          color: palette.muted,
          fontWeight: 600,
        }}
      >
        <div style={{ fontFamily: latinFont, fontSize: 20 }}>
          Unofficial community project. Not affiliated with or endorsed by
          Konami.
        </div>
        <span aria-hidden style={{ color: palette.gold }}>
          ·
        </span>
        <div style={{ fontFamily: chineseFont, fontSize: 20 }}>
          非官方爱好者项目，与科乐美无关联，未获其背书。
        </div>
      </div>
    </AbsoluteFill>
  );
};
