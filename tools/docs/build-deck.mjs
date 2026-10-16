// Bộ thẻ ôn (flashcard) sinh từ tài liệu, để ôn ngắt quãng (training/README §3, "Lịch ôn"):
//   - mọi khối "✅ Tự kiểm tra" trong docs/training: câu hỏi số N ghép với đáp án số N trong <details> ngay sau;
//   - bảng thuật ngữ GLOSSARY.md: thuật ngữ → nghĩa;
//   - ngân hàng câu hỏi T10 §5: <details><summary><b>Qn.</b> câu hỏi</summary> đáp án </details>.
// Ra file văn bản nhập được vào Anki (tab, HTML, cột tag và cột GUID: nhập lại sau khi tài liệu đổi thì
// cập nhật thẻ cũ thay vì nhân đôi). Tài liệu là nguồn duy nhất: không sửa tay file sinh ra.
// Chạy: node tools/docs/build-deck.mjs           (kiểm: file đã commit phải khớp tài liệu)
//       node tools/docs/build-deck.mjs --write   (ghi lại file)
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { repoRoot, linesWithFence, report, finish } from './lib.mjs';

const dir = path.join(repoRoot, 'docs', 'training');
const output = path.join(dir, 'deck', 'CadCoreFramework-flashcards.txt');
const write = process.argv.includes('--write');

/** Markdown của một ô → HTML đủ dùng cho thẻ: code, chữ đậm/nghiêng, link thành chữ, xuống dòng thành <br>. */
function toHtml(markdown) {
  const out = [];
  let fence = null;
  for (const line of markdown.replace(/\t/g, '    ').split('\n')) {
    if (/^\s*```/.test(line)) {
      if (fence) { out.push(`<pre>${fence.map(escape).join('\n')}</pre>`); fence = null; }
      else fence = [];
      continue;
    }
    if (fence) { fence.push(line); continue; }
    out.push(inline(line));
  }
  if (fence) out.push(`<pre>${fence.map(escape).join('\n')}</pre>`);
  return out.join('<br>').replace(/(<br>)+$/, '').replace(/^(<br>)+/, '').replace(/<br><pre>/g, '<pre>').replace(/<\/pre><br>/g, '</pre>');
}

function escape(text) {
  return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function inline(line) {
  const codes = [];
  const keep = (_, c) => { codes.push(c); return `\u0000${codes.length - 1}\u0000`; };
  // Code span hai backtick trước (`` "DOOR,`*U*" `` chứa dấu ` bên trong), rồi tới loại một backtick.
  let text = line.replace(/``\s?(.+?)\s?``/g, keep).replace(/`([^`]+)`/g, keep);
  text = escape(text)
    .replace(/!\[[^\]]*\]\([^)]*\)/g, '')
    .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
    .replace(/\*\*([^*]+)\*\*/g, '<b>$1</b>')
    .replace(/(^|[^\w*])\*([^*\s][^*]*)\*(?!\w)/g, '$1<i>$2</i>');
  return text.replace(/\u0000(\d+)\u0000/g, (_, i) => `<code>${escape(codes[+i])}</code>`);
}

/** Danh sách đánh số ở đầu dòng → { preface, items: Map(số → { text, no }) }. Dòng không đánh số nối vào mục trước. */
function numbered(lines) {
  const items = new Map();
  const preface = [];
  let current = null;
  for (const { line, no, inFence } of lines) {
    const m = !inFence && /^(\d+)\. (.*)$/.exec(line);
    if (m) { current = { text: [m[2]], no }; items.set(+m[1], current); continue; }
    if (current) current.text.push(line);
    else if (line.trim()) preface.push(line);
  }
  for (const item of items.values()) item.text = item.text.join('\n').trim();
  return { preface: preface.join('\n').trim(), items };
}

const guid = key => 'cadcore-' + crypto.createHash('sha1').update(key).digest('hex').slice(0, 12);
const cards = [];
const add = (front, back, tags, key) => cards.push([toHtml(front), toHtml(back), tags.join(' '), guid(key)]);

// ─── Khối ✅ ───────────────────────────────────────────────────────────────
const modules = fs.readdirSync(dir).filter(f => /^[CTU]\d\d_.*\.md$/.test(f)).sort();
let blocks = 0;
for (const name of modules) {
  const file = path.join(dir, name);
  const code = name.slice(0, 3);
  const lines = linesWithFence(fs.readFileSync(file, 'utf8'));
  for (let i = 0; i < lines.length; i++) {
    const h = !lines[i].inFence && /^#{2,3} ✅ (.*)$/.exec(lines[i].line);
    if (!h) continue;
    const open = lines.findIndex((l, j) => j > i && !l.inFence && /^<details><summary>.*<\/summary>\s*$/.test(l.line));
    const next = lines.findIndex((l, j) => j > i && !l.inFence && /^#{1,6} /.test(l.line));
    if (open < 0 || (next >= 0 && next < open)) { report(file, lines[i].no, 'khối ✅ không có <details> đáp án ngay sau câu hỏi'); continue; }
    const close = lines.findIndex((l, j) => j > open && !l.inFence && /^<\/details>\s*$/.test(l.line));
    if (close < 0) { report(file, lines[open].no, '<details> của khối ✅ không có </details>'); continue; }

    const questions = numbered(lines.slice(i + 1, open));
    const answers = numbered(lines.slice(open + 1, close));
    const qn = [...questions.items.keys()], an = [...answers.items.keys()];
    if (qn.length === 0) { report(file, lines[i].no, 'khối ✅ không có câu hỏi đánh số "1. …"'); continue; }
    if (qn.join() !== an.join()) {
      report(file, lines[i].no, `khối ✅ có câu hỏi [${qn}] nhưng đáp án [${an}]: mỗi câu hỏi số N cần đúng một đáp án số N`);
      continue;
    }
    // "✅ Tự kiểm tra §4" → "T02 §4"; "✅ Tự kiểm tra T02" (cuối module) → "T02, tổng hợp".
    const where = /§[\d.]+/.exec(h[1]);
    const label = where ? `${code} ${where[0]}` : `${code}, tổng hợp`;
    blocks++;
    for (const [n, q] of questions.items) {
      const front = `[${label}] ` + (questions.preface ? questions.preface + '\n' : '') + q.text;
      add(front, answers.items.get(n).text, [`CadCore::${code}`, 'CadCore::tu-kiem-tra'], `${name}#${h[1]}#${n}`);
    }
  }
}
const selfCheck = cards.length;

// ─── GLOSSARY ──────────────────────────────────────────────────────────────
const glossaryFile = path.join(dir, 'GLOSSARY.md');
let part = '';
for (const { line, no, inFence } of linesWithFence(fs.readFileSync(glossaryFile, 'utf8'))) {
  if (inFence) continue;
  const section = /^## ([A-Z])\. /.exec(line);
  if (section) { part = section[1]; continue; }
  const row = /^\| (.+?) \| (.+?) \| (.*?) \|\s*$/.exec(line);
  if (!row || /^:?-+/.test(row[1]) || row[1] === 'Thuật ngữ') continue;
  if (!part) { report(glossaryFile, no, 'dòng bảng nằm ngoài các phần A/B/C'); continue; }
  const see = row[3].trim() ? `\n*Xem: ${row[3].trim()}*` : '';
  add(row[1], row[2] + see, ['CadCore::thuat-ngu', `CadCore::thuat-ngu::${part}`], `GLOSSARY#${row[1]}`);
}
const glossary = cards.length - selfCheck;

// ─── T10 §5: ngân hàng câu hỏi ─────────────────────────────────────────────
const t10 = path.join(dir, 'T10_LABS_AND_ASSESSMENT.md');
const t10Lines = linesWithFence(fs.readFileSync(t10, 'utf8'));
const bankStart = t10Lines.findIndex(l => /^## 5\. /.test(l.line));
if (bankStart < 0) report(t10, 1, 'không thấy mục "## 5." (ngân hàng câu hỏi)');
for (let i = bankStart + 1; bankStart >= 0 && i < t10Lines.length; i++) {
  if (/^## /.test(t10Lines[i].line)) break;
  const q = /^<details><summary><b>(Q\d+)\.<\/b> (.*)<\/summary>\s*$/.exec(t10Lines[i].line);
  if (!q) continue;
  const close = t10Lines.findIndex((l, j) => j > i && /^<\/details>\s*$/.test(l.line));
  if (close < 0) { report(t10, t10Lines[i].no, `${q[1]} không có </details>`); continue; }
  const question = q[2].replace(/<code>(.*?)<\/code>/g, '`$1`').replace(/<\/?[bi]>/g, '**');
  const answer = t10Lines.slice(i + 1, close).map(l => l.line).join('\n').trim();
  add(`[T10 ${q[1]}] ${question}`, answer, ['CadCore::T10', 'CadCore::ngan-hang'], `T10#${q[1]}`);
  i = close;
}
const bank = cards.length - selfCheck - glossary;

// ─── Ghi hoặc kiểm ─────────────────────────────────────────────────────────
const seen = new Map();
for (const c of cards) {
  if (seen.has(c[3])) report(output, 1, `hai thẻ trùng GUID ${c[3]}: ${seen.get(c[3]).slice(0, 60)} / ${c[0].slice(0, 60)}`);
  seen.set(c[3], c[0]);
}
const header = [
  '#separator:tab',
  '#html:true',
  '#deck:CadCoreFramework',
  '#columns:Mặt trước\tMặt sau\tTags\tGUID',
  '#tags column:3',
  '#guid column:4',
];
const text = header.concat(cards.map(c => c.join('\t'))).join('\n') + '\n';
const rel = path.relative(repoRoot, output).split(path.sep).join('/');
if (write) {
  fs.mkdirSync(path.dirname(output), { recursive: true });
  fs.writeFileSync(output, text);
} else if (!fs.existsSync(output) || fs.readFileSync(output, 'utf8').replace(/\r\n/g, '\n') !== text) {
  report(output, 1, `bộ thẻ không khớp tài liệu: chạy "node tools/docs/build-deck.mjs --write" rồi commit ${rel}`);
}
finish(`${cards.length} thẻ (${selfCheck} từ ${blocks} khối ✅, ${glossary} thuật ngữ, ${bank} câu ngân hàng)${write ? `, đã ghi ${rel}` : ''}`);
