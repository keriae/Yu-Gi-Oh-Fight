// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { TransitionSeries, linearTiming } from '@remotion/transitions';
import { fade } from '@remotion/transitions/fade';
import { Opening } from './scenes/Opening';
import { Decks } from './scenes/Decks';
import { Editor } from './scenes/Editor';
import { Academy } from './scenes/Academy';
import { Practice } from './scenes/Practice';
import { AiProfiles } from './scenes/AiProfiles';
import { AiBattle } from './scenes/AiBattle';
import { Duel } from './scenes/Duel';
import { Menus } from './scenes/Menus';
import { Replays } from './scenes/Replays';
import { Closing } from './scenes/Closing';

export const LaunchTrailer = () => (
  <TransitionSeries>
    <TransitionSeries.Sequence name="Opening" durationInFrames={180}>
      <Opening />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Deck library" durationInFrames={270}>
      <Decks />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Deck editor" durationInFrames={270}>
      <Editor />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Academy library" durationInFrames={270}>
      <Academy />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Step-by-step guide" durationInFrames={270}>
      <Practice />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="AI duelist" durationInFrames={240}>
      <AiProfiles />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="AI versus AI" durationInFrames={240}>
      <AiBattle />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Duel action" durationInFrames={360}>
      <Duel />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Duel menus" durationInFrames={270}>
      <Menus />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Replay review" durationInFrames={240}>
      <Replays />
    </TransitionSeries.Sequence>
    <TransitionSeries.Transition
      presentation={fade()}
      timing={linearTiming({ durationInFrames: 9 })}
    />
    <TransitionSeries.Sequence name="Project link" durationInFrames={180}>
      <Closing />
    </TransitionSeries.Sequence>
  </TransitionSeries>
);
