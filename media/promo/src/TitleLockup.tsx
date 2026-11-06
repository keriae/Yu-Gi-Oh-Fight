// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { displayFont } from './style';

export const TitleLockup: React.FC<{ size: number }> = ({ size }) => (
  <div
    style={{
      fontFamily: displayFont,
      fontSize: size,
      fontWeight: 700,
      lineHeight: 1.28,
      letterSpacing: 3,
      whiteSpace: 'nowrap',
      background:
        'linear-gradient(180deg, #fff4d3 9%, #e2cb91 38%, #ae8750 62%, #ead6a4 80%, #9a753f 100%)',
      backgroundClip: 'text',
      WebkitBackgroundClip: 'text',
      color: 'transparent',
      WebkitTextStroke: '0.45px #ecd6a47a',
      filter: 'drop-shadow(0 2px 0 #050b12) drop-shadow(0 5px 10px #000a)',
    }}
  >
    Yu-Gi-Oh! Fight
  </div>
);
