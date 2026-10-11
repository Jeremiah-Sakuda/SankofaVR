#!/bin/sh
# Desktop review exports from the illustrated-theatre capture test, not headset footage.
set -eu
cd "$(dirname "$0")/.."
frames=artifacts/illustrated-theatre
out=docs/media
mkdir -p "$out"
for pair in 02-unfolded:illustrated-unfolded 04-received:illustrated-received 06-trade-response:illustrated-trade; do
  source=${pair%%:*}
  name=${pair##*:}
  ffmpeg -loglevel error -y -i "$frames/$source.png" -vf 'scale=1280:-2' -q:v 4 "$out/$name.jpg"
done
count=$(cat "$frames/handover-frame-count.txt")
case "$count" in ''|*[!0-9]*) echo 'Invalid handover frame count' >&2; exit 1;; esac
ffmpeg -loglevel error -y -framerate 12 -i "$frames/handover-%04d.png" -frames:v "$count" -c:v libx264 -crf 21 -pix_fmt yuv420p -movflags +faststart "$out/illustrated-handover.mp4"
