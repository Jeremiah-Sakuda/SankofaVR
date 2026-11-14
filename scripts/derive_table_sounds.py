#!/usr/bin/env python3
"""Cut the story table's short stage sounds from field recordings already in the repository.

The paper stage folds, turns and stands up in silence. These are two short sounds for it, taken from the
tent recording under unity/Assets/Sankofa/Art/Recorded/ (old fabric in wind): one flap for the fold and a
second of rustle for the rise. Nothing is synthesised and nothing is downloaded: each sound is a cut of a
recording whose author, licence and page are in that folder's sources.json, with the wind's rumble
filtered out, short fades and a common peak.

The cuts were placed by measuring the recording's envelope. Nobody has listened to them. The app plays a
sound only if its file is in the build, so deleting a file here silences it, and replacing a file (same
name, same folder) with a better one needs no code change.

Requires ffmpeg. Run from the repository root:

    python3 scripts/derive_table_sounds.py
"""

import hashlib
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
RECORDED = ROOT / "unity" / "Assets" / "Sankofa" / "Art" / "Recorded"
OUT = ROOT / "unity" / "Assets" / "Sankofa" / "Resources" / "Sounds"
RECORD = RECORDED / "derived-sounds.json"
PEAK_DB = -3.0

# name: (source recording, start s, length s, high-pass Hz, fade in s, fade out s, what it is for)
SOUNDS = {
    "stage-fold": ("tent.wav", 23.47, 0.62, 140, 0.015, 0.18, "the stage folds before it turns"),
    "stage-rise": ("tent.wav", 26.16, 1.05, 140, 0.06, 0.35, "the scenery stands up"),
}


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def peak_db(path):
    out = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(path), "-af", "volumedetect", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    for line in out.splitlines():
        if "max_volume" in line:
            return float(line.split("max_volume:")[1].split("dB")[0])
    raise RuntimeError(f"no level for {path}")


def graph(start, length, highpass, fade_in, fade_out):
    """The cut as an ffmpeg filter: trimmed, rumble removed, faded at both ends."""
    return (f"atrim=start={start}:end={start + length},asetpts=PTS-STARTPTS,highpass=f={highpass},"
            f"afade=t=in:d={fade_in},afade=t=out:st={length - fade_out:.3f}:d={fade_out}")


def render(name, source, start, length, highpass, fade_in, fade_out):
    src = RECORDED / source
    out = OUT / f"{name}.wav"
    temp = OUT / f"{name}.tmp.wav"
    plain = ["-map_metadata", "-1", "-fflags", "+bitexact", "-flags:a", "+bitexact", "-c:a", "pcm_s16le"]
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(src),
                    "-af", graph(start, length, highpass, fade_in, fade_out), *plain, str(temp)], check=True)
    gain = PEAK_DB - peak_db(temp)
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(temp),
                    "-af", f"volume={gain:.2f}dB", *plain, str(out)], check=True)
    temp.unlink()
    return out, gain


def main():
    sources = {r["file"]: r for r in json.loads((RECORDED / "sources.json").read_text(encoding="utf-8"))["recordings"]}
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    for name, (source, start, length, highpass, fade_in, fade_out, purpose) in SOUNDS.items():
        origin = sources[source]
        if sha256(RECORDED / source) != origin["sha256"]:
            raise RuntimeError(f"{source} is not the recording sources.json describes")
        out, gain = render(name, source, start, length, highpass, fade_in, fade_out)
        records.append({
            "file": f"{name}.wav",
            "for": purpose,
            "source": source,
            "source_sha256": origin["sha256"],
            "title": origin["title"], "author": origin["author"], "licence": origin["licence"], "page": origin["page"],
            "edit": (f"{length:g} s from {start:g} s, high-pass {highpass} Hz, fades {fade_in:g} s in and {fade_out:g} s out, "
                     f"level {gain:+.1f} dB to a peak of {PEAK_DB:g} dBFS"),
            "sha256": sha256(out),
        })
        print(f"{name:11} {origin['licence']:8} {records[-1]['edit']}")
    RECORD.write_text(json.dumps({
        "about": ("Short sounds cut by scripts/derive_table_sounds.py from the field recordings in this folder; nothing "
                  "synthesised. The cuts were placed by measurement and have not been listened to. They live in "
                  "Resources/Sounds; the app plays a sound only when its file is there."),
        "sounds": records,
    }, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{len(records)} files in {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
