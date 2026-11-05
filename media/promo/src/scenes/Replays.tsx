// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { TransitionSeries, linearTiming } from '@remotion/transitions';
import { fade } from '@remotion/transitions/fade';
import { ScreenScene } from './ScreenScene';
export const Replays = () => (
  <TransitionSeries>
    <TransitionSeries.Sequence
      name="Saved replay library"
      durationInFrames={93}
    >
      <ScreenScene
        src="full-replay-zh.mp4"
        start={12}
        top={0}
        chapter="06 / REVIEW"
        english="Replay the duel. Discover the next idea."
        chinese="回看真实对局，发现下一种可能"
        detail="Pause · Speed · Card inspection"
      />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence
      name="Recorded duel playback"
      durationInFrames={156}
    >
      <ScreenScene
        src="full-replay-zh.mp4"
        start={92}
        top={0}
        chapter="06 / REVIEW"
        english="Replay the duel. Discover the next idea."
        chinese="回看真实对局，发现下一种可能"
        detail="Pause · Speed · Card inspection"
      />
    </TransitionSeries.Sequence>
  </TransitionSeries>
);
