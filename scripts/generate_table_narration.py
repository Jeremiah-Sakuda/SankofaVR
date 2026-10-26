#!/usr/bin/env python3
"""Record the story table's pending chapters in the same griot voice as its existing recordings.

The table app finds a chapter's recording by the hash of its exact text, under
unity/Assets/Sankofa/Resources/Narration/personal-table/p-<first 16 hex>.mp3. The manifest
unity/Assets/Sankofa/Content/personal-table.narration.json lists every bundled chapter; entries with
"status": "pending-recording" have approved text and no audio yet, and play as captions. The .NET test
PersonalStoryVoiceTests.NarrationManifestMatchesEveryBundledChapterAndItsRecording keeps the manifest
in step with the composer.

Uses Google Cloud Text-to-Speech with the caller's gcloud login and the manifest's own model, voice and
direction, so new chapters match the earlier ones:

    python3 scripts/generate_table_narration.py --dry-run
    python3 scripts/generate_table_narration.py

Generated narration is a development voice until it passes listening review; see docs/asset-register.md.
"""

import argparse
import datetime
import hashlib
import json
import re
import subprocess
import sys
import uuid
from pathlib import Path

import generate_narration as narration

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = "unity/Assets/Sankofa/Content/personal-table.narration.json"
FOLDER = "unity/Assets/Sankofa/Resources/Narration/personal-table"


def pending(manifest):
    """Chapters whose text is approved but which have no recording yet."""
    return [entry for entry in manifest["chapters"] if entry.get("status") == "pending-recording"]


def write_meta(clip):
    """Gives a new clip the same import settings as its recorded siblings, with its own asset GUID."""
    sibling = next((m for m in sorted(clip.parent.glob("p-*.mp3.meta")) if m.name != clip.name + ".meta"), None)
    if sibling is None:
        return None
    text = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, sibling.read_text(encoding="utf-8"), count=1, flags=re.M)
    meta = clip.with_name(clip.name + ".meta")
    meta.write_text(text, encoding="utf-8")
    return meta


def record(entry, manifest, folder, access):
    text = entry["text"]
    digest = hashlib.sha256(text.encode("utf-8")).hexdigest()
    if digest != entry["text_sha256"] or entry["filename"] != "p-" + digest[:16] + ".mp3":
        raise RuntimeError(f"{entry['chapter_id']}: manifest text and hash disagree; run the .NET tests first")
    audio = narration.synthesize(text, manifest["model"], manifest["voice"], manifest["direction"], access)
    clip = folder / entry["filename"]
    wav = clip.with_suffix(".candidate.wav")
    candidate = clip.with_suffix(".candidate.mp3")
    wav.write_bytes(audio)
    try:
        # Matches the existing table recordings: 24 kHz mono, 32 kbps MP3.
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-ac", "1", "-ar", "24000", "-c:a", "libmp3lame", "-b:a", "32k", str(candidate)], check=True)
        seconds = narration.duration(candidate)
        maximum = min(75, max(15, len(text.split()) * 0.75 + 10))
        if not 0.5 <= seconds <= maximum:
            raise RuntimeError(f"{entry['chapter_id']}: clip duration {seconds:.2f}s outside 0.5..{maximum:.1f}s; retry")
        candidate.replace(clip)
    finally:
        wav.unlink(missing_ok=True)
        candidate.unlink(missing_ok=True)
    if not clip.with_name(clip.name + ".meta").exists():
        write_meta(clip)
    data = clip.read_bytes()
    entry.pop("status", None)
    entry.update({
        "audio_sha256": hashlib.sha256(data).hexdigest(), "provider": manifest["provider"], "model": manifest["model"],
        "voice": manifest["voice"], "date": datetime.date.today().isoformat(), "length_seconds": round(seconds, 3),
        "bytes": len(data), "sample_rate_hertz": 24000, "channels": 1, "synthesis_attempts": 1,
    })


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--dry-run", action="store_true", help="list pending chapters without contacting a provider")
    args = parser.parse_args()
    path = ROOT / MANIFEST
    manifest = json.loads(path.read_text(encoding="utf-8"))
    todo = pending(manifest)
    print(f"{len(todo)} pending chapter(s): " + ", ".join(e["chapter_id"] for e in todo), file=sys.stderr)
    if args.dry_run or not todo:
        return
    access = narration.token()
    failed = []
    for entry in todo:
        try:
            record(entry, manifest, ROOT / FOLDER, access)
            print("voiced", entry["chapter_id"], file=sys.stderr)
        except (RuntimeError, subprocess.CalledProcessError) as error:
            failed.append(entry["chapter_id"])
            print("FAILED", entry["chapter_id"], str(error)[:200], file=sys.stderr)
    if not pending(manifest):
        manifest.pop("pending_note", None)
    manifest["updated_utc"] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    if failed:
        print("Still pending (captions only): " + ", ".join(failed), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
