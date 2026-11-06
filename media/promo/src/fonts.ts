// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { loadFont } from '@remotion/fonts';
import { staticFile } from 'remotion';
import { useEffect, useState } from 'react';

const ready = Promise.all([
  loadFont({
    family: 'Fight Promo Serif Latin',
    url: staticFile('fonts/FightPromoSerifLatin-600.woff2'),
    weight: '600',
  }),
  loadFont({
    family: 'Fight Promo Serif SC',
    url: staticFile('fonts/FightPromoSerifSC-600.woff2'),
    weight: '600',
  }),
]);

export const usePromoFonts = () => {
  const [loaded, setLoaded] = useState(false);
  useEffect(() => {
    let mounted = true;
    ready.then(() => {
      if (mounted) setLoaded(true);
    });
    return () => {
      mounted = false;
    };
  }, []);
  return loaded;
};
