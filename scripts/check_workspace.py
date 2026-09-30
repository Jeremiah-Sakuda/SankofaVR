#!/usr/bin/env python3
"""Validate tracked documentation and protections for local-only material."""
from pathlib import Path
import argparse
import re
import subprocess
import sys
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]
COMPETITION_BRIEF = "HACKATHON.local.md"
LOCAL_ONLY = (
    "references/hackathon/", "references/ideation/", "data/", "recordings/",
    "results/", "artifacts/", "vendor/", "unity/Assets/LocalOnly/",
    "unity/Assets/StreamingAssets/",
)


def git(*args):
    return subprocess.run(
        ["git", *args], cwd=ROOT, capture_output=True, text=True, check=False
    )


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--competition", action="store_true",
        help="Also require a nonempty local competition brief (never included in CI).",
    )
    args = parser.parse_args()
    result = git("ls-files", "-z")
    if result.returncode:
        print("Run inside an initialised Git repository.", file=sys.stderr)
        return 1
    tracked = {name for name in result.stdout.split("\0") if name}
    if not tracked:
        print("Stage the intended files before checking the workspace.", file=sys.stderr)
        return 1
    errors = []
    for name in sorted(tracked):
        path = ROOT / name
        if name == COMPETITION_BRIEF or name.startswith(LOCAL_ONLY):
            errors.append(f"Local-only file in index: {name}")
        if git("check-ignore", "--no-index", "-q", name).returncode == 0:
            errors.append(f"Ignored file in index: {name}")
        if not path.is_file():
            errors.append(f"Tracked file missing: {name}")
            continue
        if not name.endswith(".md"):
            continue
        text = path.read_text(encoding="utf-8")
        if text.count("```") % 2:
            errors.append(f"Unclosed Markdown fence: {name}")
        for match in re.finditer(r"\]\((?:<([^>]+)>|([^\s)]+))\)", text):
            target = match.group(1) or match.group(2)
            if target.startswith("#"):
                continue
            parsed = urlsplit(target)
            if parsed.scheme in ("https", "http", "mailto", "tel"):
                continue
            if parsed.scheme or target.startswith("/"):
                errors.append(f"Nonportable link in {name}: {target}")
                continue
            resolved = (path.parent / unquote(parsed.path)).resolve()
            if not resolved.is_relative_to(ROOT):
                errors.append(f"Link leaves repository in {name}: {target}")
            elif not resolved.exists():
                errors.append(f"Broken local link in {name}: {target}")
            elif resolved.is_file() and resolved.relative_to(ROOT).as_posix() not in tracked:
                errors.append(f"Link to untracked material in {name}: {target}")
    protected = (
        COMPETITION_BRIEF,
        "references/hackathon/rules.txt", "references/hackathon/description.txt",
        "references/ideation/original-chat.txt", "data/session.json",
        "recordings/session.wav", "results/report.md", "artifacts/build.apk",
        "vendor/model.glb", ".env", ".env.local", "signing.keystore",
        "unity/Library/cache.bin", "unity/Assets/Resources/DevAgentSettings.asset",
        "unity/Assets/LocalOnly/raw.fbx", "unity/Assets/LocalOnly.meta",
        "unity/Assets/StreamingAssets/personal.json", "dotnet/Story/bin/Story.dll",
    )
    for name in protected:
        if git("check-ignore", "--no-index", "-q", name).returncode:
            errors.append(f"Missing ignore protection: {name}")
    for name in ("unity/Assets/Main.unity", "unity/Assets/Main.unity.meta",
                 "unity/Packages/manifest.json", "unity/ProjectSettings/ProjectVersion.txt"):
        if git("check-ignore", "--no-index", "-q", name).returncode == 0:
            errors.append(f"Required future Unity source would be ignored: {name}")
    if args.competition:
        try:
            brief = (ROOT / COMPETITION_BRIEF).read_text(encoding="utf-8")
            if not brief.strip():
                errors.append(f"Empty {COMPETITION_BRIEF}; follow docs/development.md local competition setup.")
        except (OSError, UnicodeError):
            errors.append(f"Missing or unreadable {COMPETITION_BRIEF}; follow docs/development.md local competition setup.")
    if errors:
        print("\n".join(errors), file=sys.stderr)
        return 1
    print(f"Workspace check passed: {len(tracked)} tracked files; document links and local-only exclusions verified.")
    if args.competition:
        print("Local competition brief is present. Read it; this check does not establish competition compliance.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
