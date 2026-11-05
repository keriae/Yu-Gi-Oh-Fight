// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { AbsoluteFill, interpolate, staticFile } from 'remotion';
import { Audio } from '@remotion/media';
import { TransitionSeries, linearTiming } from '@remotion/transitions';
import { fade } from '@remotion/transitions/fade';
import { ScreenScene } from './ScreenScene';
export const Menus = () => (
  <AbsoluteFill>
    <Audio
      src={staticFile('footage/features-zh.mp4')}
      trimBefore={18 * 30}
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
          top={0}
          chapter="05 / DUEL"
          english="Stay focused. Every detail, within reach."
          chinese="设置、日志与卡片详情，融入统一的对战体验"
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
          zoom={1.5}
          left={-960}
          top={-140}
          chapter="05 / DUEL"
          english="Stay focused. Every detail, within reach."
          chinese="设置、日志与卡片详情，融入统一的对战体验"
        />
      </TransitionSeries.Sequence>
      <TransitionSeries.Transition
        presentation={fade()}
        timing={linearTiming({ durationInFrames: 9 })}
      />
      <TransitionSeries.Sequence name="Full card details" durationInFrames={96}>
        <ScreenScene
          src="menus-zh.mp4"
          start={62}
          top={0}
          chapter="05 / DUEL"
          english="Stay focused. Every detail, within reach."
          chinese="设置、日志与卡片详情，融入统一的对战体验"
        />
      </TransitionSeries.Sequence>
    </TransitionSeries>
  </AbsoluteFill>
);
