"""Offline checks for levelling the story table's recordings; no clip is decoded, changed or played."""
import unittest
from pathlib import Path
from unittest.mock import patch

import level_table_narration as levels


class LevelTests(unittest.TestCase):
    def test_steps_go_to_the_nearest_step_and_never_past_full_scale(self):
        cases = [(-19.3, -3.0, 0), (-18.6, -3.0, 0), (-18.5, -3.0, -1), (-16.0, -2.4, -2), (-21.8, -7.4, 2),
                 (-20.2, -1.1, 0),   # a step up would put its peak above full scale
                 (-20.9, -2.3, 1), (-18.2, 0.1, -1), (-19.3, 0.4, -1), (-30.0, -1.0, 0)]
        for loudness, peak, expected in cases:
            with self.subTest(loudness=loudness, peak=peak):
                steps = levels.steps_for(loudness, peak)
                self.assertEqual(steps, expected)
                self.assertLessEqual(peak + steps * levels.STEP_DB, levels.CEILING_DBFS)

    def test_shared_and_pending_recordings(self):
        manifest = {"chapters": [{"filename": "a.mp3", "chapter_id": "one"}, {"filename": "a.mp3", "chapter_id": "two"},
                                 {"filename": "b.mp3", "status": "pending-recording"}],
                    "lines": [{"filename": "c.mp3", "line_id": "welcome"}],
                    "archived_chapters": [{"filename": "old.mp3"}]}
        files = levels.current(manifest)
        self.assertEqual(sorted(files), ["a.mp3", "c.mp3"], "pending and archived clips are left alone")
        self.assertEqual(len(files["a.mp3"]), 2, "two chapters that tell the same words share one recording")

    def test_a_dry_run_measures_and_changes_nothing(self):
        manifest = {"chapters": [{"filename": "a.mp3"}], "lines": []}
        with patch.object(levels, "measure", return_value=(-16.0, -2.4)), patch.object(levels.subprocess, "run") as run:
            rows = levels.level(manifest, Path("/nonexistent"), apply=False)
        run.assert_not_called()
        self.assertEqual(rows, [("a.mp3", -16.0, -2, -19.0, -5.4)])
        self.assertEqual(manifest, {"chapters": [{"filename": "a.mp3"}], "lines": []})

    def test_the_committed_recordings_sit_within_two_steps_of_each_other(self):
        import json
        manifest = json.loads((levels.ROOT / levels.MANIFEST).read_text(encoding="utf-8"))
        measured = [entry["loudness_lufs"] for entries in levels.current(manifest).values() for entry in entries]
        self.assertGreaterEqual(len(measured), 23)
        self.assertLessEqual(max(measured) - min(measured), 2 * levels.STEP_DB, "run scripts/level_table_narration.py after a new recording")
        self.assertTrue(all(entry["true_peak_dbfs"] <= levels.CEILING_DBFS for entries in levels.current(manifest).values() for entry in entries))


if __name__ == "__main__":
    unittest.main()
