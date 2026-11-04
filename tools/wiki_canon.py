"""Paired hosted/local SCP Wiki references, without changing the reference checkout."""
from collections import defaultdict
from functools import lru_cache
from html import escape
import json
from pathlib import Path
import re
from urllib.parse import unquote, urlsplit

HOSTS = {'scp-wiki.wikidot.com', 'www.scp-wiki.net', 'scp-wiki.net', 'scpwiki.com', 'www.scpwiki.com'}
ANCHOR = re.compile(r'<a\b(?P<before>[^>]*?)href="(?P<url>[^"]+)"(?P<after>[^>]*)>(?P<label>.*?)</a>', re.S)


def canon_key(url):
    parsed = urlsplit(url)
    if parsed.hostname not in HOSTS: return None
    return unquote(parsed.path).strip('/').lower()


def build_canon(pages, add, archive, out, slug, digest, link, listing, revision):
    registry = {}
    hashes = {}
    for collection in ['items', 'tales', 'goi', 'hubs']:
        path = archive / 'docs/data/scp' / collection / 'index.json'
        text = path.read_text(); hashes[path] = digest(text)
        for key, meta in json.loads(text).items():
            if not isinstance(meta, dict): continue
            url = meta.get('url') or 'https://scp-wiki.wikidot.com/' + str(meta.get('link', key)).lower()
            canonical = canon_key(url)
            if canonical:
                registry[canonical] = (collection, key, meta)
    references = defaultdict(set)
    for page, (_, body, _, _, _) in list(pages.items()):
        for match in ANCHOR.finditer(body):
            key = canon_key(match['url'])
            if key: references[key].add(page)
    out_records = out / 'canon-records'; out_records.mkdir(exist_ok=True)
    entries = {}

    @lru_cache(maxsize=2)
    def load(path):
        text = path.read_text(); hashes[path] = digest(text)
        return json.loads(text)

    def hosted_link(url, label):
        return link(url, label).replace('<a ', '<a data-canon-paired="true" data-canon-hosted="true" ')

    for key, backlinks in sorted(references.items()):
        if key not in registry:
            entries[key] = {'hosted': 'https://scp-wiki.wikidot.com/' + key,
                            'available': False, 'reason': 'No matching record in the indexed local collections.'}
            continue
        collection, record_key, meta = registry[key]
        base = (archive / 'docs/data/scp' / collection).resolve()
        path = (base / meta.get('content_file', 'index.json')).resolve()
        if not path.is_relative_to(base): raise ValueError('Archive path escapes collection')
        record = load(path).get(record_key)
        if not isinstance(record, dict):
            entries[key] = {'hosted': meta['url'], 'available': False, 'reason': 'Indexed record is missing from its shard.'}
            continue
        url = 'canon-' + slug(key) + '.html'
        json_url = 'canon-records/' + slug(key) + '.json'
        record_text = json.dumps(record, ensure_ascii=False, indent=2) + '\n'
        (out / json_url).write_text(record_text)
        pointer = '/' + record_key.replace('~', '~0').replace('/', '~1')
        source = path.relative_to(archive).as_posix()
        entry = {'available': True, 'hosted': meta.get('url', 'https://scp-wiki.wikidot.com/' + key),
                 'local_page': url, 'local_json': json_url, 'original_json': '../scp-api/' + path.relative_to(archive).as_posix(), 'collection': collection, 'record_key': record_key,
                 'dataset_path': source, 'json_pointer': pointer, 'archive_revision': revision,
                 'shard_sha256': hashes[path], 'record_sha256': digest(record_text), 'backlinks': sorted(backlinks)}
        entries[key] = entry
        title = str(meta.get('title') or record_key)
        body = '<h1>' + escape(title) + ' — local SCP Wiki snapshot</h1>'
        body += '<aside class="canon-source"><strong>Canon source comparison</strong><div>' + hosted_link(entry['hosted'], 'Hosted SCP Wiki ↗') + ' · ' + link(json_url, 'Local JSON record') + ' · ' + link('canon-index.html', 'Canon source directory') + '</div><p>This is the local archive snapshot, not a live copy. Open the hosted page alongside it to compare; no automatic equality or freshness claim is made.</p></aside>'
        body += '<h2>Back to the game wiki</h2>' + listing(link(p, pages[p][0]) for p in sorted(backlinks))
        related = set(re.findall(r'\bSCP-(\d+)(?![\w-])', str(record.get('raw_source') or record.get('raw_content') or ''), re.I))
        game_urls = ['scp-' + n + '.html' for n in related if 'scp-' + n + '.html' in pages]
        if game_urls:
            body += '<h2>Related in-game SCP entries</h2>' + listing(link(p, pages[p][0]) for p in sorted(game_urls))
        body += '<h2>Snapshot provenance</h2><dl>'
        for name, value in [('Creator metadata', str(meta.get('creator', 'not supplied'))), ('Archive revision', revision),
                            ('Original dataset file', '../scp-api/' + source), ('Record key', record_key),
                            ('JSON pointer', pointer), ('Dataset SHA-256', hashes[path]), ('Extracted record SHA-256', digest(record_text))]:
            body += '<dt>' + escape(name) + '</dt><dd><code>' + escape(value) + '</code></dd>'
        body += '</dl><p>' + link(entry['original_json'], 'Open original dataset JSON shard') + ' (find record key <code>' + escape(record_key) + '</code>; use the wiki server for HTTP access). ' + link(json_url, 'Open exact single-record JSON') + '.</p><p>The linked JSON is an unchanged single-record extraction from that dataset file. It is packaged here so it works when serving just the game wiki and when reading offline. The cloned archive is left untouched.</p>'
        body += '<h2>Archived article text</h2><p>Escaped source text; archived scripts and HTML are never executed. Article statements are source fiction, not automatically adopted game mechanics.</p>'
        raw = record.get('raw_source') or record.get('raw_content')
        body += '<pre class="canon-text">' + escape(str(raw)) + '</pre>' if raw else '<p>This archive record has no article body. Use its metadata and hosted source.</p>'
        body += '<p>Source: ' + link(entry['hosted'], title) + '. Archive creator metadata: ' + escape(str(meta.get('creator', 'not supplied'))) + '. SCP Wiki text is available under <a href="https://creativecommons.org/licenses/by-sa/3.0/">CC BY-SA 3.0</a>; consult the article for full credits and any asset-specific notices. No linked article images are imported.</p>'
        add(url, title + ' — local canon snapshot', body, 'Local canon', 'Archived source', source + ' # ' + record_key)
    def decorate(body):
        def replace(match):
            if 'data-canon-paired' in match['before'] + match['after']: return match.group(0)
            key = canon_key(match['url'])
            if key is None: return match.group(0)
            entry = entries.get(key)
            # Keep original source text and fragments intact; label every off-site canon link.
            hosted = '<a ' + match['before'] + 'href="' + escape(match['url'], quote=True) + '"' + match['after'] + ' data-canon-hosted="true" title="Hosted SCP Wiki; external site">' + match['label'] + '</a>'
            companion = link(entry['local_page'], 'Local snapshot') + ' · ' + link(entry['local_json'], 'JSON') if entry and entry['available'] else '<span title="No matching record in this archive snapshot">Local copy unavailable</span>'
            return '<span class="canon-link">' + hosted + ' <span class="canon-link-options">[Hosted ↗ · ' + companion + ']</span></span>'
        return ANCHOR.sub(replace, body)
    # Only replace body fields. Search retains its concise original terms, avoiding boilerplate duplication.
    for page, (title, body, category, status, evidence) in list(pages.items()):
        if category == 'SCP':
            number = page.removeprefix('scp-').removesuffix('.html')
            entry = entries.get('scp-' + number)
            if entry and entry['available']:
                source_block = '<aside class="canon-source"><strong>Canon source comparison</strong><div>' + hosted_link(entry['hosted'], 'Hosted SCP Wiki ↗') + ' · ' + link(entry['local_page'], 'Read local snapshot') + ' · ' + link(entry['local_json'], 'Local JSON record') + '</div><p>Game adaptation below; source fiction in the linked records. Local snapshot revision: <code>' + escape(revision[:12]) + '</code>.</p></aside>'
                body = re.sub(r'(</h1>)', lambda m: m[0] + source_block, body, count=1)
        pages[page] = (title, decorate(body), category, status, evidence)
    rows = []
    for key, entry in sorted(entries.items()):
        local = link(entry['local_page'], 'Read snapshot') + ' · ' + link(entry['local_json'], 'JSON record') if entry['available'] else escape(entry['reason'])
        rows.append('<tr><td>' + link(entry['hosted'], key) + '</td><td>' + local + '</td></tr>')
    body = '<h1>Canon source directory</h1><p>Hosted SCP Wiki sources cited by this game wiki, paired with exact local archive records where available. Local records carry the checkout revision, original shard path, JSON pointer, hashes and backlinks to the citing game pages. Missing records are explicitly marked; no local content is invented.</p><p>Both copies may differ. Open the hosted article and local snapshot alongside each other to inspect changes. The archive is not updated by this build.</p><table><thead><tr><th>Hosted source ↗</th><th>Local dataset copy</th></tr></thead><tbody>' + ''.join(rows) + '</tbody></table>'
    add('canon-index.html', 'Canon source directory', decorate(body), 'Directory')
    (out / 'canon-index.json').write_text(json.dumps(entries, ensure_ascii=False, indent=2))
    return {p.relative_to(archive).as_posix(): h for p, h in hashes.items()}, len([e for e in entries.values() if e['available']])
