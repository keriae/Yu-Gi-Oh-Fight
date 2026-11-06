// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { createHash } from 'node:crypto';
import { readFile, stat, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = fileURLToPath(new URL('../', import.meta.url));
const media = path.resolve(root, '../../docs/media');
const chapters = JSON.parse(
  await readFile(path.join(root, 'chapters.json'), 'utf8'),
);
const footagePath = path.join(root, 'footage.json');
const footage = JSON.parse(await readFile(footagePath, 'utf8'));

const timestamp = (seconds) => {
  const total = Math.round(seconds * 1000);
  return `${String(Math.floor(total / 3600000)).padStart(2, '0')}:${String(Math.floor(total / 60000) % 60).padStart(2, '0')}:${String(Math.floor(total / 1000) % 60).padStart(2, '0')},${String(total % 1000).padStart(3, '0')}`;
};
for (const language of ['en', 'zh']) {
  const key = language === 'en' ? 'english' : 'chinese';
  const subtitles = chapters
    .map(
      (chapter, index) =>
        `${index + 1}\n${timestamp(chapter.from_seconds)} --> ${timestamp(chapter.to_seconds)}\n${chapter[key]}\n`,
    )
    .join('\n');
  await writeFile(
    path.join(media, `launch-trailer-${language}.srt`),
    subtitles,
  );
}
for (const name of ['launch-trailer.mp4', 'launch-trailer.srt', 'preview.jpg'])
  delete footage.outputs[name];
for (const language of ['en', 'zh']) {
  for (const name of [
    `launch-trailer-${language}.mp4`,
    `launch-trailer-${language}.srt`,
    `preview-${language}.jpg`,
  ]) {
    const file = path.join(media, name);
    const bytes = await readFile(file);
    footage.outputs[name] = {
      repository_path: `docs/media/${name}`,
      size_bytes: (await stat(file)).size,
      sha256: createHash('sha256').update(bytes).digest('hex'),
    };
  }
}
await writeFile(footagePath, `${JSON.stringify(footage, null, 2)}\n`);
