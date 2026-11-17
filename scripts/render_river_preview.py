#!/usr/bin/env python3
"""Add a reproducible editorial sound sketch to Unity's river-threshold desktop render.

This is a preview mix, not captured Unity audio or headset evidence. The default music
is an explicitly authorised local audition with unverified provenance; it is never
downloaded or included in a player by this script. No audio is played on this computer.
Requires ffmpeg on PATH, or the optional imageio-ffmpeg package used on this workstation.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parent.parent


def clip(words):
    digest = hashlib.sha256(words.encode("utf-8")).hexdigest()[:16]
    return ROOT / "unity/Assets/Sankofa/Resources/Narration/personal-table" / ("p-" + digest + ".mp3")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ffmpeg", default=shutil.which("ffmpeg"))
    parser.add_argument("--music", type=Path, default=ROOT / "unity/Assets/LocalOnly/RiverThreshold/intro-music.mp3")
    args = parser.parse_args()
    if not args.ffmpeg:
        import imageio_ffmpeg
        args.ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    directory = ROOT / "artifacts/river-threshold"
    recordings = ROOT / "unity/Assets/Sankofa/Art/Recorded"
    inputs = [directory / "river-threshold-silent.mp4", args.music,
              recordings / "small-fire.wav", recordings / "brook.wav",
              recordings / "RiverThreshold/pot-touch.wav", recordings / "RiverThreshold/water-stir.wav",
              ROOT / "unity/Assets/Sankofa/Resources/Sounds/stage-rise.wav",
              clip("Come. Sit with me."),
              clip("The people in it are imagined. What is documented, I will mark.")]
    for path in inputs:
        if not path.is_file():
            raise FileNotFoundError(f"Preview input missing: {path}")
    command = [args.ffmpeg, "-v", "error", "-y"]
    for i, path in enumerate(inputs):
        if i in (2, 3):
            command += ["-stream_loop", "-1"]
        command += ["-i", str(path)]
    filters = [
        "[1:a]atrim=duration=41,asetpts=PTS-STARTPTS,volume='if(between(t,0.4,3.6)+between(t,18,27),0.14,0.38)':eval=frame,afade=t=in:d=2,afade=t=out:st=38:d=3[music]",
        "[2:a]atrim=duration=50,asetpts=PTS-STARTPTS,volume=0.10,afade=t=in:d=2[fire]",
        "[3:a]atrim=duration=13,asetpts=PTS-STARTPTS,volume=0.12,afade=t=in:d=3,adelay=37000:all=1[brook]",
        "[4:a]volume=0.24,adelay=35000:all=1[pot]",
        "[5:a]volume=0.25,adelay=36200:all=1[water]",
        "[6:a]volume=0.18,adelay=12000:all=1[paper]",
        "[7:a]volume=0.85,adelay=400:all=1[come]",
        "[8:a]volume=0.85,adelay=18000:all=1[truth]",
        "[music][fire][brook][pot][water][paper][come][truth]amix=inputs=8:normalize=0:duration=longest,alimiter=limit=0.95,afade=t=out:st=48:d=2[mix]",
    ]
    output = directory / "river-threshold-preview.mp4"
    command += ["-filter_complex", ";".join(filters), "-map", "0:v:0", "-map", "[mix]",
                "-c:v", "copy", "-c:a", "aac", "-b:a", "160k", "-t", "50", "-movflags", "+faststart", str(output)]
    subprocess.run(command, check=True)
    manifest = {
        "evidence": "Stationary Unity desktop render with scripted input and an editorial preview audio mix; not recorded Unity output, simulator or Quest footage.",
        "music_status": "User-authorised local audition; composer, human/AI origin and release rights unverified.",
        "audio_acceptance": "No listening or device acceptance asserted. New explanatory title cards are captioned, not newly voiced.",
        "inputs": [{"path": str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),
                    "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in inputs],
        "filters": filters,
        "output": output.name,
    }
    (directory / "preview-mix.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(output)


if __name__ == "__main__":
    main()
