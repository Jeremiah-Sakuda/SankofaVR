#!/usr/bin/env python3
"""Voice every line of a story package as the griot, and time its captions to the audio.

Uses Google Cloud Text-to-Speech (Gemini TTS) with the caller's gcloud login, billed to the
Sankofa project. Each line becomes one MP3 clip under unity/Assets/Sankofa/Resources/Narration/<package>/,
the package's "audio" key points at it and "seconds" becomes the clip's measured length.
A manifest beside the package records the text, model, voice and direction of every clip, so re-runs only
regenerate lines whose text or settings changed.

    python3 scripts/generate_narration.py unity/Assets/Sankofa/Content/enkare-nairobi.json
    python3 scripts/generate_narration.py <package> --only welcome.sit,welcome.griot --force

Generated narration is a development voice until it passes listening review; see
docs/asset-register.md.
"""

import argparse
import base64
import concurrent.futures
import hashlib
import json
import re
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROJECT = "gen-lang-client-0010438999"
MODEL = "gemini-3.1-flash-tts-preview"
VOICE = "Kore"
# The original web Sankofa griot's direction, kept verbatim at the user's request.
DIRECTION = (
    "Read the following passage aloud in the warm, unhurried cadence of a West African griot — "
    "resonant, deep, with occasional pauses for emphasis. Let the words breathe. "
    "This is oral storytelling, not news reading."
)
RATE = 24000


def lines(package):
    """Every spoken line in the package, wherever it lives."""
    for key in ("welcome", "welcome_back", "choose", "early_return"):
        yield from package.get(key, [])
    for beat in package["beats"]:
        yield from beat["lines"]
        invitation = beat.get("invitation")
        if invitation:
            yield from invitation["accepted"]
            yield from invitation["default"]
        for question in beat.get("questions", []):
            yield from question["lines"]


def fingerprint(text, model, voice, direction):
    return hashlib.sha256("\n".join([text, model, voice, direction]).encode("utf-8")).hexdigest()


def token():
    return subprocess.run(["gcloud", "auth", "print-access-token"], check=True, capture_output=True, text=True).stdout.strip()


def synthesize(text, model, voice, direction, access):
    body = {
        "input": {"prompt": direction, "text": text},
        "voice": {"languageCode": "en-US", "name": voice, "modelName": model},
        "audioConfig": {"audioEncoding": "LINEAR16", "sampleRateHertz": RATE},
    }
    request = urllib.request.Request(
        "https://texttospeech.googleapis.com/v1/text:synthesize",
        data=json.dumps(body).encode("utf-8"),
        headers={"Authorization": "Bearer " + access, "x-goog-user-project": PROJECT, "Content-Type": "application/json"},
    )
    for attempt in range(3):
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                return base64.b64decode(json.load(response)["audioContent"])
        except urllib.error.HTTPError as error:
            if error.code not in (429, 500, 503) or attempt == 2:
                raise RuntimeError(f"{error.code}: {error.read()[:300]!r}") from error


def duration(clip):
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(clip)],
                         check=True, capture_output=True, text=True).stdout
    return float(out.strip())


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("package", type=Path)
    parser.add_argument("--model", default=MODEL)
    parser.add_argument("--voice", default=VOICE)
    parser.add_argument("--only", default="", help="comma-separated line ids")
    parser.add_argument("--force", action="store_true", help="regenerate even if unchanged")
    parser.add_argument("--clip-namespace", help="new immutable folder for a revision; preserves audio referenced by older kept visits")
    args = parser.parse_args()

    package = json.loads(args.package.read_text(encoding="utf-8"))
    namespace = args.clip_namespace or package["id"]
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]*", namespace):
        parser.error("clip namespace must be a single alphanumeric/dot/dash/underscore name")
    folder = ROOT / "unity/Assets/Sankofa/Resources/Narration" / namespace
    folder.mkdir(parents=True, exist_ok=True)
    # Kept beside the package, outside Resources, so it never ships in a build.
    manifest_path = args.package.with_name(namespace + ".narration.json")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.exists() else {}
    only = {i for i in args.only.split(",") if i}
    by_id = {line["id"]: line for line in lines(package)}
    if only - by_id.keys():
        parser.error("unknown line ids: " + ", ".join(sorted(only - by_id.keys())))

    todo = []
    for line in lines(package):
        if only and line["id"] not in only:
            continue
        mark = fingerprint(line["text"], args.model, args.voice, DIRECTION)
        clip = folder / (line["id"] + ".mp3")
        if args.force or not clip.exists() or manifest.get(line["id"], {}).get("fingerprint") != mark:
            todo.append((line, mark, clip))

    access = token() if todo else None
    print(f"{len(todo)} line(s) to voice with {args.model} / {args.voice}", file=sys.stderr)

    def work(item):
        line, mark, clip = item
        try:
            audio = synthesize(line["text"], args.model, args.voice, DIRECTION, access)
        except RuntimeError as error:
            return line["id"], None, str(error)
        # Stored as MP3 (VBR quality 3), several times smaller than the PCM the service returns.
        wav = clip.with_suffix(".candidate.wav")
        candidate = clip.with_suffix(".candidate.mp3")
        wav.write_bytes(audio)
        try:
            subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-c:a", "libmp3lame", "-q:a", "3", str(candidate)], check=True)
            seconds = duration(candidate)
            # Catch runaway speech/silence before attaching it to a short caption.
            maximum = min(60, max(15, len(line["text"].split()) * 1.5 + 5))
            if not 0.5 <= seconds <= maximum:
                return line["id"], None, f"clip duration {seconds:.2f}s outside 0.5..{maximum:.1f}s; retry this line"
            candidate.replace(clip)
        finally:
            wav.unlink(missing_ok=True)
            candidate.unlink(missing_ok=True)
        return line["id"], mark, len(line["text"])

    failed = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        for line_id, mark, detail in pool.map(work, todo):
            if mark is None:
                failed.append(line_id)
                print("FAILED", line_id, detail[:200], file=sys.stderr)
                continue
            manifest[line_id] = {"fingerprint": mark, "provider": "Google Cloud Text-to-Speech",
                                 "model": args.model, "voice": args.voice, "direction": DIRECTION,
                                 "text": by_id[line_id]["text"], "characters": detail}
            print("voiced", line_id, file=sys.stderr)

    # Time every voiced line's caption and watchdog to its clip.
    for line in lines(package):
        clip = folder / (line["id"] + ".mp3")
        mark = fingerprint(line["text"], args.model, args.voice, DIRECTION)
        # A failed regeneration must never bind an old recording to revised words.
        if clip.exists() and manifest.get(line["id"], {}).get("fingerprint") == mark and line["id"] not in failed:
            line["audio"] = f"{namespace}/{line['id']}"
            line["seconds"] = round(duration(clip), 2)
            manifest.setdefault(line["id"], {})["seconds"] = line["seconds"]
        elif not only or line["id"] in only:
            line.pop("audio", None)

    manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    args.package.write_text(json.dumps(package, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    total = sum(v.get("seconds", 0) for v in manifest.values())
    print(f"{len(manifest)} clips, {total / 60:.1f} minutes of narration", file=sys.stderr)
    if failed:
        print("Not voiced (caption timing kept): " + ", ".join(failed), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
