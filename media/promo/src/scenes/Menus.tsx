// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { AbsoluteFill, interpolate, staticFile } from 'remotion';
import { Audio } from '@remotion/media';
import { TransitionSeries, linearTiming } from '@remotion/transitions';
import { fade } from '@remotion/transitions/fade';
import { ScreenScene } from './ScreenScene';
import { menuThemeRecording, usePromoLanguage } from '../language';
export const Menus = () => {
  const theme = menuThemeRecording(usePromoLanguage());
  return (
    <AbsoluteFill>
      <Audio
        src={staticFile('footage/' + theme.src)}
        trimBefore={theme.start * 30}
        volume={(f) =>
          interpolate(f, [0, 8, 260, 269], [0, 0.35, 0.35, 0], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          })
        }
      />
      <TransitionSeries>
        <TransitionSeries.Sequence
          name="Duel sound settings"
          durationInFrames={96}
        >
          <ScreenScene
            src="menus-zh.mp4"
            start={18}
            chapter="05 / DUEL"
            english="Read the duel. Make your next move count."
            chinese="读懂战局，决胜下一手"
          />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: 9 })}
        />
        <TransitionSeries.Sequence
          name="Attack and damage log"
          durationInFrames={96}
        >
          <ScreenScene
            src="menus-zh.mp4"
            start={48}
            chapter="05 / DUEL"
            english="Read the duel. Make your next move count."
            chinese="读懂战局，决胜下一手"
          />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: 9 })}
        />
        <TransitionSeries.Sequence
          name="Full card details"
          durationInFrames={96}
        >
          <ScreenScene
            src="menus-zh.mp4"
            start={62}
            chapter="05 / DUEL"
            english="Read the duel. Make your next move count."
            chinese="读懂战局，决胜下一手"
          />
        </TransitionSeries.Sequence>
      </TransitionSeries>
    </AbsoluteFill>
  );
};
