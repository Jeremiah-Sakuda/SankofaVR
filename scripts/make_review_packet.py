#!/usr/bin/env python3
"""Builds the review packets for the illustrated episode, from the recorded read-throughs and desktop captures.

Two packets, each a folder and a zip under artifacts/review-packet/ (ignored; they hold generated narration and
draft art, and go only where the user sends them):

- family: a letter, the pictures, both recorded paths, the script with the time each line is spoken, questions for
  family and community reviewers, and a pronunciation list with the time each name or word is first spoken;
- listener: one recording and the questions for someone unfamiliar with the setting, with no script or intended
  answers, so nothing steers what they say they understood.

Usage: python3 scripts/make_review_packet.py
Needs ffmpeg/ffprobe, artifacts/voice-pass (the recorded read-throughs) and artifacts/illustrated-theatre captures.
"""
import html
import json
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
VOICE = ROOT / "artifacts" / "voice-pass"
CAPTURES = ROOT / "artifacts" / "illustrated-theatre"
ART = ROOT / "unity" / "Assets" / "Sankofa" / "Art" / "IllustratedTheatre"
PACKAGE = ROOT / "unity" / "Assets" / "Sankofa" / "Content" / "illustrated-theatre-mvp.json"
OUT = ROOT / "artifacts" / "review-packet"

PICTURES = [
    ("02-unfolded", "The water at dusk, as the story opens around the listener."),
    ("03-painted-person", "Wambui arrives with the flour."),
    ("04-received", "The gourd of flour has been handed to the listener."),
    ("06-trade-response", "The flour set down between the two women."),
    ("07-keepsake-offer", "Naserian offers the bead."),
]
# Names and words whose pronunciation, spelling and use the family should check.
WORDS = ["Enkare", "Nairobi", "Naserian", "Wambui", "Maa", "Maasai", "Kikuyu", "oloirien", "Akan", "Sankofa"]

STYLE = """
body { font: 18px/1.55 Georgia, 'Times New Roman', serif; color: #2a2118; background: #fbf7f0; margin: 0; }
main { max-width: 760px; margin: 0 auto; padding: 32px 20px 64px; }
h1 { font-size: 30px; margin: 0 0 4px; } h2 { font-size: 22px; margin: 40px 0 8px; border-bottom: 1px solid #d9cbb4; padding-bottom: 4px; }
.draft { color: #8a5a2b; font-style: italic; margin: 0 0 24px; }
figure { margin: 18px 0; } img { width: 100%; border-radius: 6px; } figcaption { font-size: 15px; color: #6b5b48; }
audio { width: 100%; margin: 6px 0 18px; }
.line { display: grid; grid-template-columns: 4.2em 1fr; gap: 10px; margin: 6px 0; }
.time { font: 14px/1.9 ui-monospace, Menlo, monospace; color: #8a5a2b; }
.differs { background: #f3e7d3; border-radius: 4px; padding: 0 4px; }
ol li, ul li { margin: 8px 0; } table { border-collapse: collapse; width: 100%; font-size: 16px; }
td, th { border-bottom: 1px solid #e3d7c4; padding: 6px 8px; text-align: left; vertical-align: top; }
.note { font-size: 15px; color: #6b5b48; }
"""


def duration(path):
    out = subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(path)])
    return float(out.decode().strip())


def timeline(name):
    """(start seconds, line id, text) for each line of a recorded path, in order."""
    files = re.findall(r"file '([^']+)'", (VOICE / f"{name}.ffconcat").read_text())
    blocks = [b.strip().split("\n", 1) for b in (VOICE / f"{name}.txt").read_text().strip().split("\n\n")]
    if len(files) != len(blocks):
        sys.exit(f"{name}: {len(files)} recordings but {len(blocks)} lines")
    lines, at = [], 0.0
    for f, (line_id, text) in zip(files, blocks):
        if Path(f).stem != line_id:
            sys.exit(f"{name}: recording {Path(f).stem} does not match line {line_id}")
        lines.append((at, line_id, text.strip()))
        at += duration(f)
    return lines


def clock(seconds):
    return f"{int(seconds // 60)}:{int(seconds % 60):02d}"


def page(title, body):
    return f"<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>{html.escape(title)}</title><style>{STYLE}</style></head><body><main>{body}</main></body></html>"


def copy_audio(folder):
    (folder / "audio").mkdir(parents=True, exist_ok=True)
    shutil.copy(VOICE / "helping-guest.mp3", folder / "audio" / "a-helping.mp3")
    shutil.copy(VOICE / "quiet-guest.mp3", folder / "audio" / "b-quiet.mp3")


def listener_questions():
    return """
<ol>
<li>Who are the two women, and what did each of them want?</li>
<li>Why was the meeting difficult?</li>
<li>What changed Naserian's answer?</li>
<li>What did the listener do in the story, and what difference did it make?</li>
<li>Which parts do you think really happened, and which were made up for the story?</li>
<li>What did the bead mean?</li>
<li>Was anything confusing, or did any part drag?</li>
</ol>
<p class='note'>Please answer in your own words before reading anything else about the story. There are no wrong answers: we want to know what came across.</p>
"""


def build_listener():
    folder = OUT / "listener"
    shutil.rmtree(folder, ignore_errors=True)
    (folder / "audio").mkdir(parents=True)
    shutil.copy(VOICE / "helping-guest.mp3", folder / "audio" / "story.mp3")
    body = f"""
<h1>Sankofa: a story by the water</h1>
<p class='draft'>Draft for listening. About seven minutes.</p>
<p>Please find a quiet place and listen to this recording once, all the way through, without stopping. It is the storyteller's
voice from a virtual-reality story; in the headset, the listener sits by a fire and the story is painted around them. In this
recording the listener helps whenever they are asked.</p>
<audio controls preload='metadata' src='audio/story.mp3'></audio>
<h2>Afterwards</h2>
{listener_questions()}
"""
    (folder / "index.html").write_text(page("Sankofa: listening", body), encoding="utf-8")
    return folder


def build_family():
    folder = OUT / "family"
    shutil.rmtree(folder, ignore_errors=True)
    copy_audio(folder)
    (folder / "pictures").mkdir()
    figures = []
    for name, caption in PICTURES:
        source = CAPTURES / f"{name}.png"
        if not source.exists():
            sys.exit(f"missing capture {source}; run the illustrated capture first")
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", str(source), "-vf", "scale=1200:-2", "-q:v", "4",
                        str(folder / "pictures" / f"{name}.jpg")], check=True)
        figures.append(f"<figure><img src='pictures/{name}.jpg' alt='{html.escape(caption)}'><figcaption>{html.escape(caption)}</figcaption></figure>")
    for atlas, caption in [("cast-atlas", "The six drawings of the two women: standing, offering and after the exchange."),
                           ("scenery-atlas", "The three painted layers of the riverbank.")]:
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", str(ART / f"{atlas}.png"), "-vf", "scale=900:-2",
                        str(folder / "pictures" / f"{atlas}.png")], check=True)
        figures.append(f"<figure><img src='pictures/{atlas}.png' alt='{html.escape(caption)}'><figcaption>{html.escape(caption)}</figcaption></figure>")

    helping, quiet = timeline("accepted"), timeline("default")
    quiet_texts = {text for _, _, text in quiet}
    script = []
    for start, _, text in helping:
        mark = "" if text in quiet_texts else " class='differs'"
        script.append(f"<div class='line'><span class='time'>A {clock(start)}</span><span{mark}>{html.escape(text)}</span></div>")
    helping_texts = {text for _, _, text in helping}
    quiet_only = [f"<div class='line'><span class='time'>B {clock(s)}</span><span class='differs'>{html.escape(t)}</span></div>"
                  for s, _, t in quiet if t not in helping_texts]

    rows = []
    for word in WORDS:
        pattern = re.compile(rf"\b{re.escape(word)}\b")
        hit = next(((s, t) for s, _, t in helping if pattern.search(t)), None)
        if hit:
            rows.append(f"<tr><td><b>{html.escape(word)}</b></td><td>A {clock(hit[0])}</td><td>{html.escape(hit[1])}</td></tr>")

    version = json.loads(PACKAGE.read_text())["version"]
    body = f"""
<h1>Sankofa: What We Carry</h1>
<p class='draft'>Draft story, version {html.escape(version)}, for family and community review. Not finished, not public.</p>

<h2>What this is</h2>
<p>Sankofa is a virtual-reality story for the Meta Quest headset. The listener sits at a night fire; the fire is the storyteller, a
griot. It takes them to the stream that gave Nairobi its name, in the time before the railway, and paints the scene around them.
They can lend a hand (take the gourd of flour, set it down between the two women, receive a bead) or simply watch.</p>
<p>Wambui, a Kikuyu trader, has brought less flour than she promised. Naserian, a Maasai trader, has brought the sheep they agreed
to exchange. Wambui admits the shortfall; Naserian remembers a time Wambui waited for her; they agree to leave part of the exchange
unfinished and meet again. Wambui and Naserian, their words and their gifts, are invented. The trade and the intermarriage
between the two peoples are history.</p>
<p>Before anything goes further, it should feel right to the people it is about. That is what we are asking you.</p>

<h2>How to review</h2>
<ol>
<li>Listen to recording A all the way through (about seven minutes). In it, the listener helps whenever asked.</li>
<li>Look through the pictures.</li>
<li>Answer the questions below in any way that suits you: notes, a voice message, or a conversation.</li>
<li>If you like, listen to recording B, where the listener only watches, and to the pronunciation list.</li>
</ol>

<h2>The recordings</h2>
<p><b>A. The listener helps</b></p><audio controls preload='metadata' src='audio/a-helping.mp3'></audio>
<p><b>B. The listener watches</b></p><audio controls preload='metadata' src='audio/b-quiet.mp3'></audio>
<p class='note'>These are the storyteller's lines only, joined together. In the headset there are pauses for the listener, the
sound of the water and the fire, and optional questions. The voice is a computer voice for now.</p>

<h2>The pictures</h2>
<p class='note'>Drafts, made with an image generator. The clothing, hair and setting are placeholders until you tell us what
would be right. The sheep is not drawn yet, so for now it is only heard.</p>
{''.join(figures)}

<h2>Questions for you</h2>
<ol>
<li>Do the names Wambui and Naserian, and the way they speak, feel right for two women of that time? Would you choose other names?</li>
<li>Does the exchange make sense: banana flour, the sheep, the gourd of milk, the hide between them? What would you change?</li>
<li>Do the reasons each woman has (her household, the promise, the memory of waiting) feel true, or flattened, or too sweet?</li>
<li>Is a bead the right small gift for Naserian to give a guest? Would something else be more fitting?</li>
<li>Does anything feel out of its time, disrespectful, or wrong?</li>
<li>The clothes, hair and setting in the pictures: what would you change?</li>
<li>How are the names and words in the list below said and spelled? Are any mispronounced in the recording?</li>
<li>Is there anything you would want the story to say, or never to say?</li>
</ol>

<h2>Names and words to check</h2>
<p class='note'>The first time each is spoken in recording A.</p>
<table><tr><th>Word</th><th>Heard at</th><th>In the line</th></tr>{''.join(rows)}</table>

<h2>The script</h2>
<p class='note'>Recording A, line by line, with the time each line starts. Shaded lines differ in recording B; recording B's own
lines follow.</p>
{''.join(script)}
<h2>Lines only in recording B</h2>
{''.join(quiet_only)}
"""
    (folder / "index.html").write_text(page("Sankofa: review", body), encoding="utf-8")
    return folder


def zipped(folder):
    archive = folder.with_suffix(".zip")
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as z:
        for path in sorted(folder.rglob("*")):
            if path.is_file():
                z.write(path, Path("sankofa-" + folder.name) / path.relative_to(folder))
    return archive


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for folder in (build_family(), build_listener()):
        archive = zipped(folder)
        print(f"{folder.relative_to(ROOT)}  ({archive.stat().st_size / 1e6:.1f} MB zip)")
