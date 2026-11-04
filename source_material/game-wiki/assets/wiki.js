'use strict';
(() => {
  const input = document.getElementById('wiki-search');
  const panel = document.getElementById('search-results');
  const article = document.getElementById('article');
  const results = document.getElementById('results');
  const info = document.getElementById('search-info');
  function search() {
    const query = input.value.trim().toLowerCase();
    const params = new URLSearchParams(location.search);
    if (query) params.set('q', query); else params.delete('q');
    try { history.replaceState(null, '', location.pathname + (params.size ? '?' + params : '') + location.hash); } catch (_) { /* file:// may restrict history */ }
    panel.hidden = !query; article.hidden = !!query; results.replaceChildren();
    if (!query) { info.textContent = ''; return; }
    const words = query.split(/\s+/);
    const hits = window.WIKI_INDEX.filter(p => words.every(w => (p.title + ' ' + p.category + ' ' + p.status + ' ' + p.text).toLowerCase().includes(w)))
      .sort((a,b) => score(b,query) - score(a,query) || a.title.localeCompare(b.title));
    info.textContent = hits.length + ' matching pages' + (hits.length > 80 ? ' · showing first 80' : '');
    for (const page of hits.slice(0,80)) {
      const item = document.createElement('div'); item.className = 'result';
      const title = document.createElement('h2'); const anchor = document.createElement('a');
      anchor.href = page.url; anchor.textContent = page.title; title.append(anchor);
      const meta = document.createElement('p'); meta.textContent = page.category + (page.status ? ' · ' + page.status : '');
      item.append(title,meta); results.append(item);
    }
    if (!hits.length) { const empty = document.createElement('p'); empty.textContent = 'No matching pages. Try a designation, type name, system, or source path.'; results.append(empty); }
  }
  function score(page,q) { const t=page.title.toLowerCase(); return (t===q ? 100 : 0)+(t.includes(q) ? 30 : 0)+(page.category==='SCP' ? 5 : 0)+(page.category==='Engine system' ? 3 : 0); }
  let timer; input.addEventListener('input', () => { clearTimeout(timer); timer=setTimeout(search,120); });
  input.addEventListener('keydown', e => { if (e.key==='Escape') { input.value=''; search(); } });
  input.value=new URLSearchParams(location.search).get('q') || ''; if(input.value) search();
})();
