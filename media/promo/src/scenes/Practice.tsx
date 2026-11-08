// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { TransitionSeries, linearTiming } from '@remotion/transitions';
import { fade } from '@remotion/transitions/fade';
import { ScreenScene } from './ScreenScene';
export const Practice = () => (
  <TransitionSeries>
    <TransitionSeries.Sequence durationInFrames={140}>
      <ScreenScene
        src="tutorial-zh.mp4"
        start={2}
        chapter="03 / LEARN"
        english="Start with a real opening hand."
        chinese="固定起手，让练习有据可循"
      />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence durationInFrames={139}>
      <ScreenScene
        src="tutorial-step-zh.mp4"
        start={2}
        chapter="03 / LEARN"
        english="Learn the line, step by step."
        chinese="分步指导，把展开真正打出来"
      />
    </TransitionSeries.Sequence>
  </TransitionSeries>
);
