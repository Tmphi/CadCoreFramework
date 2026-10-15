// Parse mọi sơ đồ ```mermaid trong file .md của repo bằng chính thư viện mermaid,
// để sơ đồ sai cú pháp bị bắt ở PR thay vì hiện thành khung lỗi trên GitHub.
// Chạy: node tools/docs/check-mermaid.mjs (cần `npm ci` trong tools/docs trước)
import fs from 'node:fs';
import { JSDOM } from 'jsdom';
import { markdownFiles, linesWithFence, report, finish } from './lib.mjs';

// mermaid cần DOM (DOMPurify dùng Element, Node…); thiếu một global là mọi flowchart báo lỗi giả.
const dom = new JSDOM('<!doctype html><html><body></body></html>');
globalThis.window = dom.window;
globalThis.document = dom.window.document;
for (const k of ['Element', 'HTMLElement', 'Node', 'DocumentFragment', 'NodeFilter', 'DOMParser']) {
  globalThis[k] = dom.window[k];
}
const { default: mermaid } = await import('mermaid');
mermaid.initialize({ startOnLoad: false });

let total = 0;
for (const file of markdownFiles()) {
  const lines = linesWithFence(fs.readFileSync(file, 'utf8'));
  for (let i = 0; i < lines.length; i++) {
    if (!/^\s*```mermaid\s*$/.test(lines[i].line)) continue;
    const start = lines[i].no;
    const body = [];
    for (i++; i < lines.length && !/^\s*```\s*$/.test(lines[i].line); i++) body.push(lines[i].line);
    total++;
    try {
      await mermaid.parse(body.join('\n'));
    } catch (e) {
      report(file, start, `sơ đồ mermaid lỗi: ${String(e.message).split('\n').slice(0, 3).join(' ')}`);
    }
  }
}
finish(`${total} sơ đồ mermaid`);
