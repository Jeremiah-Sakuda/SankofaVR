#!/usr/bin/env python3
"""Explicit opt-in synthetic Google smoke; no files, private inputs or playback.

Run only with an explicitly authorized Google project. The process prints bounded
status metadata, never provider payloads, access tokens or environment values.
"""

import argparse
import json
from pathlib import Path
import sys
import time

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from backend.contracts import StoryRequest, compose_story
from backend.content import HISTORICAL
from backend.providers import Cancellation, DEFAULT_MODEL, GeminiProvider, GoogleNarrationProvider, ProviderError


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True)
    parser.add_argument("--model", default=DEFAULT_MODEL)
    parser.add_argument("--backend", choices=["developer", "vertex"], default="developer")
    parser.add_argument("--narration", action="store_true")
    parser.add_argument("--narration-only", action="store_true")
    parser.add_argument("--fixtures-dir", type=Path, help="Optional export of these public synthetic results only.")
    args = parser.parse_args()
    provider = GeminiProvider(project=args.project, model=args.model, backend=args.backend)
    stories = []
    requests = [
        ("journeys", "A fictional traveller arrives carrying a bundle for a neighbour and wonders whether to wait.", "What does the name of Nairobi refer to?"),
        ("journeys", "A fictional trader has less flour than she expected and worries about making a fair exchange.", "What is documented about a gourd?"),
    ] if not args.narration_only else []
    for interest, fragment, question in requests:
        request = StoryRequest.parse({"region": "enkare-nairobi", "period": "1850s", "interest": interest, "fragment": fragment, "question": question})
        started = time.monotonic()
        try:
            story = compose_story(request, provider.generate(request, Cancellation(45)), "generated")
        except ProviderError as error:
            print(f"Generation failed: code={error.code}; http_status={error.status}; model={args.model}")
            return 1
        stories.append(story)
        print(f"Generation passed: mode={story['mode']}; chapters={len(story['chapters'])}; interest={interest}; model={args.model}; seconds={time.monotonic() - started:.2f}")
    if stories:
        first_fiction = [c["narration"] for c in stories[0]["chapters"] if c["truth"] == "reconstructed"]
        second_fiction = [c["narration"] for c in stories[1]["chapters"] if c["truth"] == "reconstructed"]
        distinct = len(first_fiction) == len(second_fiction) == 2 and all(a != b for a, b in zip(first_fiction, second_fiction))
        print(f"Distinct generated actions: {distinct}. This checks differing text, not cultural quality or independent factual verification.")
        if not distinct:
            return 1
        if args.fixtures_dir:
            args.fixtures_dir.mkdir(parents=True, exist_ok=True)
            for index, story in enumerate(stories):
                (args.fixtures_dir / f"public-story-{index + 1}.json").write_text(json.dumps(story, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
            print("Exported public synthetic story fixtures; no raw requests or audio stored.")
    if args.narration or args.narration_only:
        started = time.monotonic()
        try:
            audio = GoogleNarrationProvider(args.project).synthesize(next(c["narration"] for c in stories[0]["chapters"] if c["truth"] == "reconstructed") if stories else HISTORICAL["C1"], Cancellation(55))
        except ProviderError as error:
            print(f"Narration failed: code={error.code}; http_status={error.status}")
            return 1
        print(f"Narration passed: content_type=audio/mpeg; bytes={len(audio)}; seconds={time.monotonic() - started:.2f}; playback=False; stored=False")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
