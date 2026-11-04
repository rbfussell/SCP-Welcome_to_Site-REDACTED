"""Meaningful item catalog checks: complete real-data rendering and rejection of classification/art drift."""
import hashlib
import json
from pathlib import Path
import shutil
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from wiki_items import REGISTRY, build_items, load_catalog


class ItemCatalogChecks(unittest.TestCase):
    def fixture(self, directory):
        root = Path(directory)
        registry = json.loads((ROOT / REGISTRY).read_text())
        for name in [REGISTRY, registry['definitionSource'], registry['inventoryArtSource'], registry['paperdollArtSource']]:
            dest = root / name
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, dest)
        return root, registry

    def test_complete_catalog_and_exact_pngs(self):
        work = ROOT / 'work/preview'
        work.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=work, prefix='item-catalog-check-') as directory:
            pages = {}
            def add(name, *values):
                self.assertNotIn(name, pages)
                pages[name] = values
            sources, hashes, count, categories = build_items(ROOT, Path(directory), add)
            index = json.loads((Path(directory) / 'item-catalog.json').read_text())
            data = json.loads((ROOT / 'src/SiteSimulation/Inventory/Content/field-kit.json').read_text())
            self.assertEqual({d['id']: d for d in data['definitions']}, {i: v['definition'] for i, v in index['items'].items()})
            self.assertEqual(count, len(index['items']))
            self.assertEqual(len(pages), count + categories + 1)
            self.assertIn('items-worn-clothing-helmets.html', pages)
            self.assertIn('Reserved category', pages['items-worn-clothing-helmets.html'][1])
            self.assertIn('storage', index['items']['coat']['capabilities'])
            self.assertIn('wearable', index['items']['coat']['capabilities'])
            self.assertNotIn('storage', index['items']['utility-hook']['capabilities'])
            self.assertTrue(index['items']['boots']['paperdollFrames'][0]['parts'][0]['frontWornCutouts'])
            for item, entry in index['items'].items():
                copied = (Path(directory) / entry['art']['url']).read_bytes()
                self.assertEqual(copied, (ROOT / entry['art']['source']).read_bytes())
                self.assertEqual(hashlib.sha256(copied).hexdigest(), entry['art']['sha256'])
                self.assertIn('viewBox=', pages[entry['url']][1])
            self.assertTrue(hashes)

    def test_missing_classification_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            root, registry = self.fixture(directory)
            registry['items'].pop('boots')
            (root / REGISTRY).write_text(json.dumps(registry))
            with self.assertRaisesRegex(ValueError, 'classify every'):
                load_catalog(root)

    def test_representative_from_another_family_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            root, registry = self.fixture(directory)
            next(c for c in registry['categories'] if c['path'] == 'worn/clothing/shoes')['representativeItem'] = 'phone'
            (root / REGISTRY).write_text(json.dumps(registry))
            with self.assertRaisesRegex(ValueError, 'representative'):
                load_catalog(root)

    def test_escaped_source_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            root, registry = self.fixture(directory)
            registry['definitionSource'] = '../outside.json'
            (root / REGISTRY).write_text(json.dumps(registry))
            with self.assertRaisesRegex(ValueError, 'escaped'):
                load_catalog(root)

    def test_invalid_art_region_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            root, registry = self.fixture(directory)
            manifest_path = root / registry['inventoryArtSource']
            art = json.loads(manifest_path.read_text())
            asset = next(a for a in art['assets'] if a['id'] == 'trousers')
            dest = manifest_path.parent / asset['file']
            shutil.copyfile(ROOT / registry['inventoryArtSource'].rsplit('/', 1)[0] / asset['file'], dest)
            asset['region'][2] = 99999
            manifest_path.write_text(json.dumps(art))
            with self.assertRaisesRegex(ValueError, 'source region'):
                load_catalog(root)


if __name__ == '__main__':
    unittest.main()
