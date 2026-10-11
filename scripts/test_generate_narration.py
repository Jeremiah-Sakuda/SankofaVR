"""Offline checks for revision audio and failure handling; never calls a provider."""
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import generate_narration as narration


class NarrationRevisionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.package = self.root / "story.json"
        self.folder = self.root / "unity/Assets/Sankofa/Resources/Narration/story-v2"
        self.folder.mkdir(parents=True)
        self.data = {"id": "story", "welcome": [
            {"id": "changed", "text": "Revised words.", "seconds": 4, "audio": "story-v2/changed"},
            {"id": "unchanged", "text": "Original words.", "seconds": 3, "audio": "story/unchanged"}], "beats": []}
        self.package.write_text(json.dumps(self.data))
        self.argv = ["generate_narration.py", str(self.package), "--clip-namespace", "story-v2", "--only", "changed"]

    def test_failed_regeneration_never_rebinds_stale_audio(self):
        (self.folder / "changed.mp3").write_bytes(b"old recording")
        manifest = self.root / "story-v2.narration.json"
        manifest.write_text(json.dumps({"changed": {"fingerprint": "old text"}}))
        with patch.object(narration, "ROOT", self.root), patch("sys.argv", self.argv), \
             patch.object(narration, "token", return_value="test-token"), \
             patch.object(narration, "synthesize", side_effect=RuntimeError("provider unavailable")), \
             patch.object(narration, "duration") as duration:
            with self.assertRaises(SystemExit):
                narration.main()
            duration.assert_not_called()
        lines = json.loads(self.package.read_text())["welcome"]
        self.assertNotIn("audio", lines[0])
        self.assertEqual(lines[1]["audio"], "story/unchanged")
        self.assertEqual((self.folder / "changed.mp3").read_bytes(), b"old recording")

    def test_cached_revision_needs_no_credentials_and_preserves_shared_audio(self):
        (self.folder / "changed.mp3").write_bytes(b"revision recording")
        mark = narration.fingerprint("Revised words.", narration.MODEL, narration.VOICE, narration.DIRECTION)
        (self.root / "story-v2.narration.json").write_text(json.dumps({"changed": {"fingerprint": mark}}))
        with patch.object(narration, "ROOT", self.root), patch("sys.argv", self.argv), \
             patch.object(narration, "token") as token, \
             patch.object(narration, "duration", return_value=5.25):
            narration.main()
            token.assert_not_called()
        lines = json.loads(self.package.read_text())["welcome"]
        self.assertEqual(lines[0]["audio"], "story-v2/changed")
        self.assertEqual(lines[0]["seconds"], 5.25)
        self.assertEqual(lines[1], self.data["welcome"][1])

    def test_unknown_line_stops_before_authentication(self):
        with patch.object(narration, "ROOT", self.root), patch("sys.argv", self.argv[:-1] + ["typo"]), \
             patch.object(narration, "token") as token:
            with self.assertRaises(SystemExit):
                narration.main()
            token.assert_not_called()

    def test_runaway_recording_is_rejected_before_replacing_the_previous_clip(self):
        old = self.folder / "changed.mp3"
        old.write_bytes(b"previous recording")
        def convert(args, **kwargs):
            Path(args[-1]).write_bytes(b"runaway recording")
        with patch.object(narration, "ROOT", self.root), patch("sys.argv", self.argv), \
             patch.object(narration, "token", return_value="test-token"), \
             patch.object(narration, "synthesize", return_value=b"wav"), \
             patch.object(narration.subprocess, "run", side_effect=convert), \
             patch.object(narration, "duration", return_value=103):
            with self.assertRaises(SystemExit):
                narration.main()
        self.assertEqual(old.read_bytes(), b"previous recording")
        self.assertNotIn("audio", json.loads(self.package.read_text())["welcome"][0])
        self.assertEqual(list(self.folder.glob("*.candidate.*")), [])


if __name__ == "__main__":
    unittest.main()
