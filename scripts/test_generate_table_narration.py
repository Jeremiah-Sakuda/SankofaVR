"""Offline checks for recording the story table's pending chapters; never calls a provider."""
import hashlib
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import generate_table_narration as table


def entry(text, status="pending-recording"):
    digest = hashlib.sha256(text.encode("utf-8")).hexdigest()
    item = {"chapter_id": "c." + digest[:4], "text": text, "text_sha256": digest, "filename": "p-" + digest[:16] + ".mp3"}
    if status:
        item["status"] = status
    return item


class TableNarrationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.manifest = {"provider": "P", "model": "M", "voice": "V", "direction": "D",
                         "chapters": [entry("Recorded words.", None), entry("New words.")]}

    def test_only_pending_chapters_are_selected(self):
        self.assertEqual([e["text"] for e in table.pending(self.manifest)], ["New words."])

    def test_shipped_manifest_lists_the_new_perspectives_as_pending_or_recorded(self):
        manifest = json.loads((table.ROOT / table.MANIFEST).read_text(encoding="utf-8"))
        ids = {e["chapter_id"] for e in manifest["chapters"]}
        self.assertTrue({"journeys.late", "trade-household.expected", "place-water.traveller"} <= ids)
        for item in table.pending(manifest):
            self.assertNotIn("audio_sha256", item)

    def test_new_clip_meta_copies_import_settings_with_a_new_guid(self):
        sibling = self.folder / "p-0000000000000000.mp3.meta"
        sibling.write_text("fileFormatVersion: 2\nguid: " + "a" * 32 + "\nAudioImporter:\n  forceToMono: 1\n")
        clip = self.folder / "p-1111111111111111.mp3"
        clip.write_bytes(b"x")
        meta = table.write_meta(clip).read_text()
        self.assertIn("forceToMono: 1", meta)
        self.assertNotIn("a" * 32, meta)

    def test_mismatched_text_is_refused_before_contacting_the_provider(self):
        item = entry("Approved words.")
        item["text"] = "Edited words."
        with patch.object(table.narration, "synthesize") as synthesize:
            with self.assertRaises(RuntimeError):
                table.record(item, self.manifest, self.folder, "token")
            synthesize.assert_not_called()

    def test_failed_render_leaves_the_chapter_pending_without_audio(self):
        item = self.manifest["chapters"][1]
        with patch.object(table.narration, "synthesize", side_effect=RuntimeError("unavailable")):
            with self.assertRaises(RuntimeError):
                table.record(item, self.manifest, self.folder, "token")
        self.assertEqual(item["status"], "pending-recording")
        self.assertFalse((self.folder / item["filename"]).exists())


if __name__ == "__main__":
    unittest.main()
