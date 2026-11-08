// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import {
  AbsoluteFill,
  CanvasImage,
  Interactive,
  interpolate,
  staticFile,
  useCurrentFrame,
} from 'remotion';
import { Audio } from '@remotion/media';
import { chineseFont, font, latinFont, palette } from '../style';
import { usePromoFonts } from '../fonts';
import { menuThemeRecording, usePromoLanguage } from '../language';

export const Closing = () => {
  const frame = useCurrentFrame();
  const language = usePromoLanguage();
  const theme = menuThemeRecording(language);
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
        src={staticFile('footage/' + theme.src)}
        trimBefore={theme.start * 30}
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
      <CanvasImage
        src={staticFile('brand.png')}
        style={{
          width: 1080,
          height: (1080 * 718) / 1708,
          marginTop: -100,
          opacity: interpolate(frame, [0, 15], [0, 1], {
            extrapolateRight: 'clamp',
          }),
        }}
      />
      <Interactive.Div
        name="Project link"
        style={{
          fontFamily: latinFont,
          fontSize: 32,
          fontWeight: 600,
          color: palette.gold,
          marginTop: 64,
          whiteSpace: 'nowrap',
        }}
      >
        https://github.com/keriae/Yu-Gi-Oh-Fight
      </Interactive.Div>
      <Interactive.Div
        name="Project notice"
        style={{
          position: 'absolute',
          bottom: 82,
          fontFamily: language === 'en' ? latinFont : chineseFont,
          fontSize: 24,
          whiteSpace: 'nowrap',
          color: palette.muted,
          fontWeight: 600,
        }}
      >
        {language === 'en'
          ? 'Unofficial, non-commercial fan project.'
          : '非官方、非商业爱好者项目。'}
      </Interactive.Div>
    </AbsoluteFill>
  );
};
