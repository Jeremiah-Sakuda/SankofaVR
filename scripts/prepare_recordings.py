#!/usr/bin/env python3
"""Prepare Sankofa's field recordings for Unity from the raw Freesound downloads.

The raw files are the public HQ previews of the sounds listed in RECORDINGS, downloaded with the user's
approval into the ignored vendor/freesound/ folder. This script only edits them: it trims, folds stereo
to mono (sources are placed in the scene), sets a common level, and crossfades long ambiences into
seamless loops or cuts single events (bleats). Nothing is synthesised. It writes the results to
unity/Assets/Sankofa/Art/Recorded/ with sources.json (author, licence, page, edit, SHA-256 of the raw
and processed files).

Output is 16-bit WAV, since MP3's encoder padding would put a gap at every loop point; Unity compresses it for
the build. Detailed sounds keep their bandwidth; distant beds are stored at 32 kHz. Requires ffmpeg. Run from
the repository root:

    python3 scripts/prepare_recordings.py
"""

import hashlib
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
RAW = ROOT / "vendor" / "freesound"
OUT = ROOT / "unity" / "Assets" / "Sankofa" / "Art" / "Recorded"

CC0 = "CC0 1.0"
BY3 = "CC BY 3.0"
BY4 = "CC BY 4.0"

# key: (author, Freesound id, title, licence, where it was recorded)
RECORDINGS = {
    "hearth-fire": ("matucha", 170247, "campfire_01.wav", BY4, "close-up campfire"),
    "small-fire": ("SterckxS", 541572, "campfire sound", BY3, "a dying garden campfire"),
    "crickets": ("selcukartut", 504882, "crickets", CC0, "Maasai Mara, Kenya"),
    "maasai-nature": ("selcukartut", 504670, "Maasai Camp NatureSounds Africa", CC0, "Maasai Mara, Kenya"),
    "wind-grass": ("tim.kahn", 628538, "AMBRurl_wind in dry grass_TK_SASSMKH8020.flac", BY4, "Plush, Oregon"),
    "brook": ("celldroid", 692752, "Babbling Brook HQ Seamless Wav Loop.wav", BY4, "a small forest brook"),
    "spikes": ("visualasylum", 329780, "Hammering Metal Spikes", BY4, "hammer on metal spikes"),
    "tent": ("bruno.auzet", 527434, "windy tent.wav", CC0, "old fabric tent in desert wind, Palmyra, Syria"),
    "bleat": ("Yuval", 210511, "sheep bleat outdoors", CC0, "sheep in a field"),
    "bells": ("BonnyOrbit", 400785, "Swiss hills with animals with bells.WAV", CC0, "Entlebuch, Switzerland"),
    "matatu": ("blacknoutproductions", 789915, "Matatu Stage Sound", CC0, "matatu stage at Railways, Nairobi, Kenya"),
    "city-far": ("felix.blume", 685791, "City far away with slight wind in dry grass", CC0, "29 Palms, California"),
}

# output name: (raw key, kind, start s, length s, crossfade s, peak dB)
# kind "loop": the segment plus a crossfade, folded into a seamless loop of the given length.
# kind "cut": a single event, with short fades in and out.
OUTPUTS = {
    "hearth-fire": ("hearth-fire", "loop", 4.0, 60.0, 3.0, -3.0),
    "small-fire": ("small-fire", "loop", 1.0, 38.0, 3.0, -3.0),
    "crickets": ("crickets", "loop", 0.5, 22.0, 2.5, -3.0),
    "maasai-nature": ("maasai-nature", "loop", 0.5, 34.0, 2.5, -3.0),
    "wind-grass": ("wind-grass", "loop", 120.0, 60.0, 4.0, -3.0),
    "brook": ("brook", "loop", 0.0, 19.5, 0.0, -3.0),
    "spikes": ("spikes", "cut", 0.25, 10.6, 0.0, -3.0),
    "tent": ("tent", "loop", 2.0, 60.0, 3.0, -3.0),
    "bleat-1": ("bleat", "cut", 1.25, 1.3, 0.0, -3.0),
    "bleat-2": ("bleat", "cut", 5.95, 1.3, 0.0, -3.0),
    "bleat-3": ("bleat", "cut", 12.9, 1.05, 0.0, -3.0),
    "bleat-4": ("bleat", "cut", 21.5, 1.4, 0.0, -3.0),
    "bells": ("bells", "loop", 0.5, 29.0, 2.5, -3.0),
    "matatu": ("matatu", "loop", 3.0, 90.0, 4.0, -3.0),
    "city-far": ("city-far", "loop", 30.0, 60.0, 4.0, -3.0),
}


# Close, crisp sounds keep full bandwidth; distant beds need less.
DETAILED = {"hearth-fire", "small-fire", "brook", "spikes", "bleat"}


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def peak_db(path):
    out = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(path), "-af", "volumedetect", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    for line in out.splitlines():
        if "max_volume" in line:
            return float(line.split("max_volume:")[1].split("dB")[0])
    raise RuntimeError(f"no level for {path}")


def render(name, raw_key, kind, start, length, fade, target):
    src = RAW / f"{raw_key}.mp3"
    if not src.exists():
        raise FileNotFoundError(f"{src} is missing; download the approved preview first")
    out = OUT / f"{name}.wav"
    mono = "pan=mono|c0=0.5*c0+0.5*c1"
    if kind == "loop" and fade > 0:
        # Head [0, F) faded in, mixed with the tail [L, L+F) faded out, then the body [F, L): the end of the body
        # runs into the tail's material, which the loop's start now carries, so the seam is continuous.
        graph = (
            f"[0:a]atrim=start={start}:end={start + length + fade},asetpts=PTS-STARTPTS,{mono},asplit=3[a][b][c];"
            f"[a]atrim=0:{fade},asetpts=PTS-STARTPTS,afade=t=in:d={fade}[head];"
            f"[b]atrim={fade}:{length},asetpts=PTS-STARTPTS[body];"
            f"[c]atrim={length}:{length + fade},asetpts=PTS-STARTPTS,afade=t=out:d={fade}[tail];"
            f"[head][tail]amix=inputs=2:normalize=0[seam];[seam][body]concat=n=2:v=0:a=1[out]"
        )
    elif kind == "loop":
        graph = f"[0:a]atrim=start={start}:end={start + length},asetpts=PTS-STARTPTS,{mono}[out]"
    else:
        edge = min(0.05, length / 6)
        graph = (f"[0:a]atrim=start={start}:end={start + length},asetpts=PTS-STARTPTS,{mono},"
                 f"afade=t=in:d={edge},afade=t=out:st={length - edge}:d={edge}[out]")
    temp = OUT / f"{name}.tmp.wav"
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(src), "-filter_complex", graph,
                    "-map", "[out]", str(temp)], check=True)
    gain = target - peak_db(temp)
    rate = "44100" if raw_key in DETAILED else "32000"
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(temp), "-af", f"volume={gain:.2f}dB",
                    "-ar", rate, "-c:a", "pcm_s16le", str(out)], check=True)
    temp.unlink()
    return out, gain


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    for name, (raw_key, kind, start, length, fade, target) in OUTPUTS.items():
        author, sound_id, title, licence, where = RECORDINGS[raw_key]
        out, gain = render(name, raw_key, kind, start, length, fade, target)
        edit = (f"{'seamless loop' if kind == 'loop' else 'single event'} of {length:g} s from {start:g} s"
                + (f", {fade:g} s crossfade" if kind == "loop" and fade else "")
                + f", folded to mono, level {gain:+.1f} dB")
        records.append({
            "file": f"{name}.wav",
            "title": title,
            "author": author,
            "licence": licence,
            "page": f"https://freesound.org/people/{author}/sounds/{sound_id}/",
            "recorded": where,
            "source_file": f"vendor/freesound/{raw_key}.mp3 (Freesound HQ preview)",
            "source_sha256": sha256(RAW / f"{raw_key}.mp3"),
            "edit": edit,
            "sha256": sha256(out),
        })
        print(f"{name:14} {licence:10} {edit}")
    (OUT / "sources.json").write_text(json.dumps({
        "about": "Field recordings from Freesound, edited by scripts/prepare_recordings.py; nothing synthesised.",
        "recordings": records,
    }, indent=2, ensure_ascii=False) + "\n")
    print(f"{len(records)} files in {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
