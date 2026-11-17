import hashlib
import tempfile
import unittest
from pathlib import Path

from audit_gallery_assets import inspect_file


class AssetDigestEvidenceTests(unittest.TestCase):
    def test_missing_declaration_is_not_reported_as_verified_identity(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            asset = root / 'painting.png'
            asset.write_bytes(b'observed pixels')
            entry, problem = inspect_file(asset, None, root)
            self.assertFalse(problem)
            self.assertTrue(entry['exists'])
            self.assertEqual(entry['sha256'], hashlib.sha256(b'observed pixels').hexdigest())
            self.assertIsNone(entry['hash_matches'])

    def test_changed_and_missing_assets_fail_the_declared_digest(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            asset = root / 'voice.mp3'
            expected = hashlib.sha256(b'approved clip').hexdigest()
            asset.write_bytes(b'approved clip')
            entry, problem = inspect_file(asset, expected, root)
            self.assertFalse(problem)
            self.assertTrue(entry['hash_matches'])
            asset.write_bytes(b'different clip')
            entry, problem = inspect_file(asset, expected, root)
            self.assertTrue(problem)
            self.assertFalse(entry['hash_matches'])
            asset.unlink()
            entry, problem = inspect_file(asset, expected, root)
            self.assertTrue(problem)
            self.assertFalse(entry['exists'])


if __name__ == '__main__':
    unittest.main()
