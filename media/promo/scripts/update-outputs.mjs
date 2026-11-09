// Copyright © 2026 keriae. See COPYRIGHT.md for scope and third-party rights.
import { createHash } from 'node:crypto';
import { execFile } from 'node:child_process';
import { readFile, stat, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import path from 'node:path';

const root = fileURLToPath(new URL('../', import.meta.url));
const media = path.resolve(root, '../../docs/media');
const chapters = JSON.parse(
  await readFile(path.join(root, 'chapters.json'), 'utf8'),
);
const footagePath = path.join(root, 'footage.json');
const footage = JSON.parse(await readFile(footagePath, 'utf8'));
const execute = promisify(execFile);
const ffprobe = process.env.FFPROBE;
const outputRecord = async (name) => {
  const file = path.join(media, name);
  const bytes = await readFile(file);
  const record = {
    repository_path: `docs/media/${name}`,
    size_bytes: (await stat(file)).size,
    sha256: createHash('sha256').update(bytes).digest('hex'),
  };
  if (ffprobe && /\.(mp4|gif|jpg)$/.test(name)) {
    const directory = path.dirname(path.resolve(ffprobe));
    const { stdout } = await execute(ffprobe, [
      '-v', 'error', '-show_streams', '-show_format', '-of', 'json', file,
    ], {
      env: {
        ...process.env,
        DYLD_LIBRARY_PATH: [directory, process.env.DYLD_LIBRARY_PATH]
          .filter(Boolean).join(':'),
      },
    });
    const data = JSON.parse(stdout);
    const video = data.streams.find((stream) => stream.codec_type === 'video');
    const audio = data.streams.find((stream) => stream.codec_type === 'audio');
    record.metadata = {
      width: video.width,
      height: video.height,
      video_codec: video.codec_name,
    };
    if (!name.endsWith('.jpg')) {
      const [numerator, denominator] = video.avg_frame_rate.split('/').map(Number);
      record.metadata.fps = numerator / denominator;
      record.metadata.frames = Number(video.nb_frames);
      record.metadata.duration_seconds = Number(video.duration ?? data.format.duration);
      record.metadata.audio = audio ? {
        codec: audio.codec_name,
        sample_rate: Number(audio.sample_rate),
        channels: audio.channels,
      } : null;
    }
  }
  return record;
};

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
    `launch-trailer-${language}.gif`,
    `launch-trailer-${language}.srt`,
    `preview-${language}.jpg`,
  ]) {
    footage.outputs[name] = await outputRecord(name);
  }
}
footage.outputs['full-duel.mp4'] = await outputRecord('full-duel.mp4');
await writeFile(footagePath, `${JSON.stringify(footage, null, 2)}\n`);
