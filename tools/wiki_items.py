"""Read-only native-item art catalog; copy original PNG bytes and render source-region previews."""
from html import escape
import hashlib
import json
from pathlib import Path
import re
import struct

REGISTRY = 'game/Assets/items/catalog.json'


def page_id(value):
    return re.sub(r'[^a-z0-9]+', '-', value.lower()).strip('-')


def item_url(item):
    return 'item-' + page_id(item) + '.html'


def category_url(category):
    return 'items-' + category.replace('/', '-') + '.html'


def load_catalog(root):
    root = Path(root).resolve()

    def path(name):
        p = (root / name).resolve()
        if not p.is_relative_to(root) or not p.is_file():
            raise ValueError('Missing or escaped item-catalog source: ' + str(name))
        return p

    def read(name):
        def unique(pairs):
            result = {}
            for key, value in pairs:
                if key in result:
                    raise ValueError('Duplicate catalog JSON key: ' + key)
                result[key] = value
            return result
        return json.loads(path(name).read_text(), object_pairs_hook=unique)

    registry = read(REGISTRY)
    if registry.get('version') != 1 or registry.get('status') != 'authoring-catalog':
        raise ValueError('Unknown item catalog version/status')
    sources = [REGISTRY] + [registry[key] for key in ('definitionSource', 'inventoryArtSource', 'paperdollArtSource')]
    content, loose, worn = [read(name) for name in sources[1:]]
    definitions = {d['id']: d for d in content['definitions']}
    if len(definitions) != len(content['definitions']) or set(definitions) != set(registry['items']):
        raise ValueError('Catalog must classify every native definition exactly once; missing/extra/duplicate IDs')
    if len({item_url(i) for i in definitions}) != len(definitions):
        raise ValueError('Item URL collision')
    categories = {c['path']: c for c in registry['categories']}
    if len(categories) != len(registry['categories']):
        raise ValueError('Duplicate item category')
    if len({category_url(c) for c in categories}) != len(categories):
        raise ValueError('Category URL collision')
    for key, category in categories.items():
        if not re.fullmatch(r'[a-z0-9]+(?:[-/][a-z0-9]+)*', key) or not category.get('name'):
            raise ValueError('Invalid category path/name: ' + key)
        if '/' in key and key.rsplit('/', 1)[0] not in categories:
            raise ValueError('Missing category parent: ' + key)
        representative = category.get('representativeItem')
        if representative is not None:
            assigned = registry['items'].get(representative, {}).get('category', '')
            if assigned != key and not assigned.startswith(key + '/'):
                raise ValueError('Category representative must be an item in its subtree: ' + key)
    for key, entry in registry['items'].items():
        if entry.get('category') not in categories:
            raise ValueError('Unknown item category: ' + key)
    assets = {a['id']: a for a in loose['assets']}
    if len(assets) != len(loose['assets']) or set(definitions) - set(assets):
        raise ValueError('Missing/duplicate loose art binding')
    image_paths = {}
    for key in definitions:
        asset = assets[key]
        source = path(str(Path(registry['inventoryArtSource']).parent / asset['file']))
        if not source.is_relative_to(root / 'game/Assets') or source.suffix != '.png':
            raise ValueError('Catalog image must be a project PNG')
        header = source.read_bytes()[:24]
        if header[:8] != b'\x89PNG\r\n\x1a\n' or list(struct.unpack('>II', header[16:24])) != asset['size']:
            raise ValueError('PNG dimensions differ: ' + key)
        r = asset['region']
        if len(r) != 4 or any(type(n) is not int for n in r) or min(r[:2]) < 0 or min(r[2:]) <= 0 or r[0] + r[2] > asset['size'][0] or r[1] + r[3] > asset['size'][1]:
            raise ValueError('Invalid source region: ' + key)
        image_paths[key] = source
    worn_paths = {}
    for file, size in worn['images'].items():
        source = path(str(Path(registry['paperdollArtSource']).parent / file))
        if not source.is_relative_to(root / 'game/Assets') or source.suffix != '.png':
            raise ValueError('Worn catalog image must be a project PNG')
        header = source.read_bytes()[:24]
        if header[:8] != b'\x89PNG\r\n\x1a\n' or list(struct.unpack('>II', header[16:24])) != size:
            raise ValueError('Worn PNG dimensions differ: ' + file)
        worn_paths[file] = source
    return registry, definitions, categories, assets, worn, image_paths, sources, worn_paths


def build_items(root, out, add):
    root, out = Path(root).resolve(), Path(out).resolve()
    registry, definitions, categories, assets, worn, images, sources, worn_images = load_catalog(root)
    hashes = {}
    directory = out / 'assets/items'
    directory.mkdir(parents=True, exist_ok=True)
    urls = {}
    for item, source in {**images, **{'worn:' + k: v for k, v in worn_images.items()}}.items():
        data = source.read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        name = digest + '.png'
        target = directory / name
        target.write_bytes(data)  # Byte-identical originals, never cropped/repainted/re-encoded.
        hashes[source.relative_to(root).as_posix()] = digest
        hashes[target.relative_to(root).as_posix()] = digest
        urls[item] = 'assets/items/' + name
    wanted = {url.rsplit('/', 1)[1] for url in urls.values()}
    for old in directory.glob('*.png'):
        if re.fullmatch(r'[a-f0-9]{64}\.png', old.name) and old.name not in wanted:
            old.unlink()

    def link(url, text):
        return '<a href="' + escape(url, quote=True) + '">' + escape(text) + '</a>'

    def art(item):
        if item is None:
            return '<div class="item-art item-art-empty">Category artwork not assigned</div>'
        asset = assets[item]
        x, y, width, height = asset['region']
        full_width, full_height = asset['size']
        return ('<svg class="item-art" role="img" aria-label="' + escape(definitions[item]['name'], quote=True)
                + ' — loose source artwork" viewBox="' + f'{x} {y} {width} {height}'
                + '"><image href="' + urls[item] + '" width="' + str(full_width) + '" height="' + str(full_height) + '"/></svg>')

    def card(url, title, item, note):
        return '<a class="item-card" href="' + escape(url, quote=True) + '">' + art(item) + '<strong>' + escape(title) + '</strong><small>' + escape(note) + '</small></a>'

    def matches(category):
        return sorted((i for i, entry in registry['items'].items() if entry['category'] == category or entry['category'].startswith(category + '/')), key=lambda i: definitions[i]['name'])

    def breadcrumbs(category):
        pieces = category.split('/')
        return link('items.html', 'Item catalog') + ' / ' + ' / '.join(link(category_url('/'.join(pieces[:n])), categories['/'.join(pieces[:n])]['name']) for n in range(1, len(pieces) + 1))

    related = '<p>Related: ' + ' · '.join(link(url, title) for url, title in [
        ('asset-library.html', 'Asset organization and item bundles'), ('system-inventory.html', 'Inventory'),
        ('system-presentation.html', 'Presentation'), ('wearable-appearance.html', 'Wearable appearance'),
        ('worn-alpha-masks.html', 'Worn-only masks'), ('doc-docs-29-gear-data-standard-md.html', 'Gear data standard')]) + '.</p>'
    note = '<p class="notice">Authoring catalog of native prototype definitions, not actor knowledge or a finished content roster. Stats below are authored values; absent fields are not invented. Proposed example gear cards and SCP reference artwork are not native items.</p>'
    body = '<h1>Item catalog</h1>' + note + '<p>Browse type → subtype → individual item. Representative category art is selected in the catalog registry. Empty types are reserved authoring categories.</p><div class="item-cards">'
    for key in sorted(c for c in categories if '/' not in c):
        c = categories[key]
        body += card(category_url(key), c['name'], c.get('representativeItem'), str(len(matches(key))) + ' native definitions')
    body += '</div><h2>All native items</h2><div class="item-cards">'
    index = {'version': 1, 'definitionSource': registry['definitionSource'], 'categories': registry['categories'], 'items': {}}
    bindings = {b['definition']: b for b in worn['bindings']}
    for item in sorted(definitions, key=lambda i: definitions[i]['name']):
        d = definitions[item]
        category = registry['items'][item]['category']
        body += card(item_url(item), d['name'], item, categories[category]['name'])
        storage = [r for r in d.get('regions', []) if not r.get('attachment')]
        providers = [r for r in d.get('regions', []) if r.get('attachment')]
        traits = [name for name, yes in [('wearable', bool(d.get('wear'))), ('storage', bool(storage)), ('attachment', bool(d.get('attachment'))), ('mount-provider', bool(providers))] if yes]
        binding = bindings.get(item)
        attachment_bindings = [b for b in worn.get('attachments', []) if b['definition'] == item]
        detail = '<h1>' + escape(d['name']) + '</h1><p>' + breadcrumbs(category) + '</p>' + art(item) + note
        detail += '<dl><dt>Stable definition ID</dt><dd><code>' + escape(item) + '</code></dd><dt>Primary type</dt><dd>' + escape(category) + '</dd>'
        detail += '<dt>Capability cross-references</dt><dd>' + escape(', '.join(traits) or 'ordinary item') + '</dd>'
        detail += '<dt>Empty mass</dt><dd>' + str(d['massGrams']) + ' g; contents are additional</dd><dt>Packed envelope</dt><dd>' + ' × '.join(str(d[k]) for k in ['widthMm', 'lengthMm', 'thicknessMm']) + ' mm; cell count depends on destination grid</dd>'
        detail += '<dt>Base handling (authored)</dt><dd>' + escape(str(d.get('handleMs', 'not authored; runtime default')) + ' ms / ' + str(d.get('handleAp', 'not authored; runtime default')) + ' AP') + '; access and action costs apply separately</dd>'
        detail += '<dt>Tags</dt><dd>' + escape(', '.join(d.get('tags', []))) + '</dd><dt>Materials / condition / combat balance</dt><dd>Not authored as native definition fields here; no effects inferred from the image or item name.</dd></dl>'
        if d.get('wear'):
            detail += '<h2>Wear and fit metadata</h2><pre>' + escape(json.dumps(d['wear'], indent=2)) + '</pre>'
        if d.get('regions'):
            detail += '<h2>Compartments and mount surfaces</h2><table><tr><th>Region</th><th>Kind</th><th>Declared geometry</th><th>Load limit</th></tr>'
            for r in d['regions']:
                detail += '<tr><td>' + escape(r['name']) + '</td><td>' + ('attachment surface' if r.get('attachment') else 'storage compartment') + '</td><td>' + escape(str(r['columns']) + ' × ' + str(r['rows']) + ' cells @ ' + str(r['cellMm']) + ' mm; height ' + str(r['heightMm']) + ' mm') + '</td><td>' + str(r['maxMassGrams']) + ' g</td></tr>'
            detail += '</table><p>Attachment-surface grid metadata is not loose storage. Region geometry, masks, closure defaults and costs are preserved in the exact definition below.</p>'
        detail += '<h2>Artwork and authoring coverage</h2><p>' + link(urls[item], 'Full original loose-art PNG') + '; registered source region <code>' + escape(str(assets[item]['region'])) + '</code>. Source preview uses the manifest crop without editing the original PNG. This is not an in-game screenshot.</p>'
        detail += '<p>Source: <code>' + escape(images[item].relative_to(root).as_posix()) + '</code>. SHA-256: <code>' + hashes[images[item].relative_to(root).as_posix()] + '</code>.</p>'
        detail += '<p>Paperdoll: ' + ('registered for the bounded reference body/default pose; inspect binding below' if binding else 'registered as a bounded direct exterior attachment; inspect supporting provider and surface below' if attachment_bindings else 'no qualified native worn-sprite binding') + '. World outfit composition remains unimplemented.</p>'
        if binding:
            detail += '<pre>' + escape(json.dumps(binding, indent=2)) + '</pre>'
        columns = {binding['column'], binding.get('openColumn', binding['column']), *binding.get('mountColumns', {}).values()} if binding else set()
        columns.update(b['column'] for b in attachment_bindings)
        registered = [f for f in worn['frames'] if f['column'] in columns]
        worn_assets = [{'file': f['file'], 'source': worn_images[f['file']].relative_to(root).as_posix(), 'url': urls['worn:' + f['file']]} for f in registered]
        if attachment_bindings:
            detail += '<pre>' + escape(json.dumps(attachment_bindings, indent=2)) + '</pre>'
        if worn_assets:
            detail += '<p>Registered worn source sheets: ' + ' · '.join(link(a['url'], a['source']) for a in worn_assets) + '.</p>'
        if binding or attachment_bindings:
            detail += '<details><summary>Registered worn frames, views and masks</summary><pre>' + escape(json.dumps(registered, indent=2)) + '</pre></details>'
        bundle = 'game/Assets/items/' + category + '/' + item + '/'
        bundle_file = (root / bundle / 'item.art.json').resolve()
        bundle_registration = None
        if bundle_file.is_relative_to(root / 'game/Assets') and bundle_file.is_file():
            bundle_registration = json.loads(bundle_file.read_text())
            if bundle_registration.get('definition') != item:
                raise ValueError('Item bundle identity differs: ' + item)
            sources.append(bundle_file.relative_to(root).as_posix())
            provenance = bundle_file.parent / 'provenance.md'
            if provenance.is_file(): sources.append(provenance.relative_to(root).as_posix())
            detail += '<p>Registered reference-art bundle: <code>' + escape(bundle) + '</code>. ' + link('evidence-' + page_id(bundle + 'item.art.json') + '.html', 'Bundle metadata and current runtime registration') + '. Existing manifest loading remains authoritative.</p>'
        else:
            detail += '<p>Future bundle location: <code>' + escape(bundle) + '</code>. This is a migration target, not an existing relocated runtime asset.</p>'
        detail += '<h2>Exact native definition</h2><pre>' + escape(json.dumps(d, indent=2)) + '</pre><p>' + link('item-catalog.json', 'Machine-readable catalog and exact definitions') + ' · ' + link('doc-docs-46-item-asset-library-md.html', 'Naming and migration standard') + '</p>' + related
        if item in {'utility-belt', 'utility-hook', 'utility-pouch'} and (attachment_bindings or binding and binding.get('attachmentPlane')):
            detail += '<p>' + link('worn-attachment-chains.html', 'Qualified worn belt/hook/pouch chains') + ': shared composed frames, separate alpha targets and ordinary paid handling.</p>'
        if item in {'shirt', 'personnel-badge'}:
            detail += '<p>' + link('shirt-pockets-and-badge.html', 'Shirt pockets and detachable badge') + ': real compartments and clip mounting; credential door access remains planned.</p>'
        if any(part.get('pockets') for frame in registered for part in frame['parts']):
            detail += '<p>' + link('paperdoll-pocket-segments.html', 'Paperdoll pocket segments') + ': source-local contours link the illustrated pocket to its real compartment and fastening.</p>'
        if any(part.get('dropRegion') for frame in registered for part in frame['parts']):
            detail += '<p>' + link('worn-container-drops.html', 'Worn container drop targets') + ': visible registered material uses ordinary paid packing and real compartment access.</p>'
        add(item_url(item), d['name'] + ' — item', detail, 'Item', 'Native prototype definition', registry['definitionSource'] + ' #' + item)
        index['items'][item] = {'url': item_url(item), 'category': category, 'capabilities': traits, 'definition': d,
                                'art': {'url': urls[item], 'source': images[item].relative_to(root).as_posix(), 'region': assets[item]['region'], 'sha256': hashes[images[item].relative_to(root).as_posix()]}, 'paperdollBinding': binding, 'paperdollFrames': registered, 'paperdollAttachments': attachment_bindings, 'wornArt': worn_assets, 'futureBundle': bundle, 'bundleRegistration': bundle_registration}
    body += '</div><h2>Capability views</h2>'
    for trait in ['wearable', 'storage', 'attachment', 'mount-provider']:
        body += '<h3>' + escape(trait) + '</h3><p>' + ' · '.join(link(v['url'], definitions[i]['name']) for i, v in index['items'].items() if trait in v['capabilities']) + '</p>'
    body += '<p>' + link('worn-attachment-chains.html', 'Worn attachment chain qualification') + ' connects registered hook/pouch artwork to physical provider frames.</p>'
    body += '<p>' + link('worn-container-drops.html', 'Worn container drops') + ' connects bag/pouch part registrations to ordinary compartment handling.</p>'
    body += '<p>' + link('paperdoll-pocket-segments.html', 'Paperdoll pocket segments') + ' connects registered clothing contours to real compartments.</p>'
    add('items.html', 'Item catalog', body + related, 'Directory', str(len(definitions)) + ' native definitions')
    for key, category in sorted(categories.items()):
        children = [c for c in categories if c.rsplit('/', 1)[0] == key and c != key]
        detail = '<h1>' + escape(category['name']) + '</h1><p>' + breadcrumbs(key) + '</p><p>Type: <code>' + escape(key) + '</code>. ' + str(len(matches(key))) + ' native definitions in this subtree.</p>'
        detail += '<p>Representative artwork: ' + (link(item_url(category['representativeItem']), definitions[category['representativeItem']]['name']) if category.get('representativeItem') else 'not assigned; no native items are implied') + '.</p>'
        detail += '<div class="item-cards">' + ''.join(card(category_url(c), categories[c]['name'], categories[c].get('representativeItem'), str(len(matches(c))) + ' definitions') for c in sorted(children)) + '</div>'
        direct = [i for i in matches(key) if registry['items'][i]['category'] == key]
        detail += '<h2>Items in this type</h2><div class="item-cards">' + ''.join(card(item_url(i), definitions[i]['name'], i, i) for i in direct) + '</div>'
        if not direct:
            detail += '<p>No definitions assigned directly here. ' + ('Choose a subtype above.' if children else 'Reserved category; item content and artwork have not been authored.') + '</p>'
        add(category_url(key), category['name'] + ' — item type', detail + related, 'Item category', 'Authoring taxonomy')
    (out / 'item-catalog.json').write_text(json.dumps(index, indent=2) + '\n')
    return [root / name for name in sources] + [Path(__file__).resolve()], hashes, len(definitions), len(categories)
