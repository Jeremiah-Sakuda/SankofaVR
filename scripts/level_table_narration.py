#!/usr/bin/env python3
"""Bring the story table's current recordings to a common loudness without re-encoding them.

The griot's lines were synthesised one at a time and came out several decibels apart, so one line could arrive
noticeably louder or quieter than the line before it. This measures every current clip in
unity/Assets/Sankofa/Content/personal-table.narration.json (chapters and lines; archived clips are left alone),
moves each toward one target in MP3 global-gain steps of 1.5 dB, and records the new byte hashes and the measured
levels in the manifest.

A gain step changes a number in each frame's header, not the audio data. Nothing is decoded and encoded again, so
no quality is lost and a step can be undone exactly. A clip is never raised above full scale.

    python3 scripts/level_table_narration.py --dry-run
    python3 scripts/level_table_narration.py

Needs ffmpeg and mp3gain on the path. Run it again after any new recording: clips already within a step of the
target are left untouched. A measured level is not a listening review; see docs/asset-register.md.
"""

import argparse
import datetime
import hashlib
import json
import math
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = "unity/Assets/Sankofa/Content/personal-table.narration.json"
FOLDER = "unity/Assets/Sankofa/Resources/Narration/personal-table"
STEP_DB = 1.5
TARGET_LUFS = -19.3
CEILING_DBFS = 0.0


def measure(path):
    """Integrated loudness (LUFS) and true peak (dBFS) of one clip, by ITU-R BS.1770 as ffmpeg implements it."""
    out = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", str(path), "-af", "ebur128=peak=true", "-f", "null", "-"],
                         capture_output=True, text=True, check=True).stderr
    loudness = re.findall(r"I:\s+(-?[\d.]+) LUFS", out)
    peak = re.findall(r"Peak:\s+(-?[\d.]+) dBFS", out)
    if not loudness or not peak:
        raise RuntimeError(f"{path.name}: could not be measured")
    return float(loudness[-1]), float(peak[-1])


def steps_for(loudness, peak, target=TARGET_LUFS, ceiling=CEILING_DBFS):
    """Whole gain steps that bring a clip nearest the target without lifting its peak above the ceiling."""
    steps = math.floor((target - loudness) / STEP_DB + 0.5)
    while peak + steps * STEP_DB > ceiling:
        steps -= 1
    return steps


def current(manifest):
    """Current entries by file. Two chapters that tell the same words share one recording."""
    files = {}
    for entry in manifest.get("chapters", []) + manifest.get("lines", []):
        if entry.get("status") == "pending-recording":
            continue
        files.setdefault(entry["filename"], []).append(entry)
    return files


def level(manifest, folder, apply=True, target=TARGET_LUFS):
    """Returns one row per file: (filename, loudness before, steps, loudness after, peak after)."""
    rows = []
    for name, entries in sorted(current(manifest).items()):
        clip = folder / name
        loudness, peak = measure(clip)
        steps = steps_for(loudness, peak, target)
        if steps and apply:
            subprocess.run(["mp3gain", "-q", "-c", "-s", "s", "-g", str(steps), str(clip)], check=True, capture_output=True)
            after, after_peak = measure(clip)
        else:
            after, after_peak = (loudness + steps * STEP_DB, peak + steps * STEP_DB) if steps else (loudness, peak)
        if apply:
            digest = hashlib.sha256(clip.read_bytes()).hexdigest()
            for entry in entries:
                entry["audio_sha256"] = digest
                entry["bytes"] = clip.stat().st_size
                entry["loudness_lufs"] = after
                entry["true_peak_dbfs"] = after_peak
                entry["level_steps"] = entry.get("level_steps", 0) + steps
        rows.append((name, loudness, steps, after, after_peak))
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--dry-run", action="store_true", help="Measure and report; change no file.")
    args = parser.parse_args()
    for tool in ("ffmpeg", "mp3gain"):
        if not shutil.which(tool):
            print(f"{tool} is not installed.", file=sys.stderr)
            return 1
    path = ROOT / MANIFEST
    manifest = json.loads(path.read_text(encoding="utf-8"))
    rows = level(manifest, ROOT / FOLDER, apply=not args.dry_run)
    for name, before, steps, after, peak in rows:
        print(f"{name}  {before:6.1f} LUFS  {steps:+d} step{'s' if abs(steps) != 1 else ''}  ->  {after:6.1f} LUFS, peak {peak:5.1f} dBFS")
    levels = [row[3] for row in rows]
    print(f"{len(rows)} clips, {min(levels):.1f} to {max(levels):.1f} LUFS" + (" (nothing changed)" if args.dry_run else ""))
    if args.dry_run:
        return 0
    manifest["levelling"] = {
        "target_lufs": TARGET_LUFS, "step_db": STEP_DB,
        "method": "MP3 global-gain steps (mp3gain -g); no decoding or re-encoding. Measured with ffmpeg ebur128.",
        "note": "Levels are measurements, not a listening review. level_steps on each clip undoes exactly.",
    }
    manifest["updated_utc"] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
