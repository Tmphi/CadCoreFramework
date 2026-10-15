// Kiểm mọi link tương đối trong file .md của repo: file đích phải tồn tại (đúng cả chữ hoa/thường,
// vì GitHub phân biệt), và #anchor phải khớp một heading của file đích theo cách GitHub tạo anchor.
// Chạy: node tools/docs/check-links.mjs
import fs from 'node:fs';
import path from 'node:path';
import { markdownFiles, linesWithFence, headingAnchors, removedByTemplate, report, finish } from './lib.mjs';

const anchorCache = new Map();
function anchorsOf(file) {
  if (!anchorCache.has(file)) {
    anchorCache.set(file, new Set(headingAnchors(fs.readFileSync(file, 'utf8')).map(h => h.anchor)));
  }
  return anchorCache.get(file);
}

// Windows không phân biệt hoa/thường nên existsSync("readme.md") vẫn true; so từng đoạn đường dẫn với tên thật.
const dirCache = new Map();
function existsExactCase(target) {
  if (!fs.existsSync(target)) return false;
  const parts = path.relative(path.parse(target).root, target).split(path.sep).filter(Boolean);
  let dir = path.parse(target).root;
  for (const part of parts) {
    if (!dirCache.has(dir)) dirCache.set(dir, new Set(fs.readdirSync(dir)));
    if (!dirCache.get(dir).has(part)) return false;
    dir = path.join(dir, part);
  }
  return true;
}

let total = 0, removed = 0;
for (const file of markdownFiles()) {
  for (const { line, no, inFence } of linesWithFence(fs.readFileSync(file, 'utf8'))) {
    if (inFence) continue;
    const noInlineCode = line.replace(/`[^`]*`/g, '');
    for (const m of noInlineCode.matchAll(/\]\(([^)\s]+)\)/g)) {
      const href = m[1];
      if (/^(https?:|mailto:)/.test(href)) continue;
      total++;
      const [p, frag] = href.split('#');
      const target = p ? path.resolve(path.dirname(file), decodeURIComponent(p)) : file;
      if (removedByTemplate(target)) { removed++; continue; }
      if (!existsExactCase(target)) report(file, no, `không có file: ${href}`);
      else if (frag && target.endsWith('.md') && !anchorsOf(target).has(decodeURIComponent(frag))) {
        report(file, no, `không có anchor: ${href}`);
      }
    }
  }
}
finish(`${total} link${removed ? ` (bỏ qua ${removed} link vào thư mục template đã xóa)` : ''}`);
