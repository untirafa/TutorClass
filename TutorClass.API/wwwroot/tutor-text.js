'use strict';
// Small, text-only Markdown renderer: model output is never assigned to innerHTML.
globalThis.TutorClassText = (() => {
  function math(value) {
    let text = String(value);
    for (let i = 0; i < 12; i++) {
      const next = text.replace(/\\(?:dfrac|tfrac|frac)\s*\{([^{}]*)\}\s*\{([^{}]*)\}/g, '($1)/($2)')
        .replace(/\\sqrt\s*\{([^{}]*)\}/g, '√($1)')
        .replace(/\\(?:text|mathrm|mathbf)\s*\{([^{}]*)\}/g, '$1');
      if (next === text) break;
      text = next;
    }
    const symbols = {times:'×', cdot:'·', div:'÷', pm:'±', leq:'≤', geq:'≥', neq:'≠', approx:'≈',
      pi:'π', theta:'θ', alpha:'α', beta:'β', Delta:'Δ', infty:'∞', sum:'∑', to:'→'};
    return text.replace(/\\(left|right)(?=[()[\]{}|])/g, '')
      .replace(/\\([A-Za-z]+)\b/g, (all, command) => symbols[command] || all)
      .replace(/\\\(([\s\S]*?)\\\)/g, '$1').replace(/\\\[([\s\S]*?)\\\]/g, '$1')
      .replace(/\$\$([\s\S]*?)\$\$/g, '$1').replace(/(^|[^\w])\$([^$\n]+)\$/g, '$1$2')
      .replace(/([_^])\{([^{}]*)\}/g, '$1($2)');
  }
  function inline(parent, source) {
    const tokens = /(`[^`\n]+`|\*\*[^*\n]+\*\*|__[^_\n]+__|(?<![\w*])\*(?!\s)[^*\n]+(?<!\s)\*(?![\w*]))/g;
    let start = 0;
    for (const match of source.matchAll(tokens)) {
      parent.append(document.createTextNode(math(source.slice(start, match.index))));
      const code = match[0].startsWith('`');
      const italic = !code && !match[0].startsWith('**') && !match[0].startsWith('__');
      const node = document.createElement(code ? 'code' : italic ? 'em' : 'strong');
      node.textContent = code ? match[0].slice(1, -1) : math(match[0].slice(italic ? 1 : 2, italic ? -1 : -2));
      parent.append(node); start = match.index + match[0].length;
    }
    parent.append(document.createTextNode(math(source.slice(start))));
  }
  function render(target, source) {
    target.replaceChildren();
    let code = null, list = null;
    for (const line of String(source).replace(/\r\n?/g, '\n').split('\n')) {
      if (/^\s*```/.test(line)) {
        if (code) code = null;
        else { const pre = document.createElement('pre'); code = document.createElement('code'); pre.append(code); target.append(pre); }
        list = null; continue;
      }
      if (code) { code.append(document.createTextNode(line + '\n')); continue; }
      if (/^\s*(?:\\\[|\\\]|\$\$)\s*$/.test(line)) continue;
      if (!line.trim()) { list = null; continue; }
      if (/^\s{0,3}(?:(?:-\s*){3,}|(?:\*\s*){3,}|(?:_\s*){3,})$/.test(line)) {
        list = null; target.append(document.createElement('hr')); continue;
      }
      const heading = line.match(/^\s*#{1,6}\s+(.+)$/);
      const item = line.match(/^\s*(?:([-+*])\s+|\d+[.)]\s+)(.+)$/);
      let block;
      if (item) {
        const tag = item[1] ? 'ul' : 'ol';
        if (!list || list.tagName.toLowerCase() !== tag) { list = document.createElement(tag); target.append(list); }
        block = document.createElement('li'); list.append(block); inline(block, item[2]);
      } else {
        list = null; block = document.createElement(heading ? 'h4' : 'p');
        inline(block, heading ? heading[1] : line); target.append(block);
      }
    }
  }
  return Object.freeze({math, render});
})();
