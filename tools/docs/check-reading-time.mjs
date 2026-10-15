// Kiểm dòng "⏱ Đọc" và nhãn 🎯/🔍/📖 của các module trong docs/training (quy ước ở training/README §5):
//   - Mỗi mục "## " phải có đúng một nhãn, đặt ở cuối dòng Mục lục trỏ tới nó (hoặc ngay sau một link
//     trỏ tới nó, như thẻ của T03). Không cần nhãn: "Mục lục" và các mục "✅ …" (tính là 🎯).
//   - Module không gắn nhãn nào (C01–C05) thì thẻ phải ghi "mọi mục đều 🎯".
//   - Số phút trên thẻ = số từ (kể cả code) ÷ 100 từ/phút, làm tròn 5 phút, tối thiểu 5:
//     "≈ X phút" cho cả trang, "phần 🎯 ≈ Y phút" cho đầu trang + mục 🎯 + Mục lục + ✅.
// Chạy: node tools/docs/check-reading-time.mjs
import fs from 'node:fs';
import path from 'node:path';
import { repoRoot, linesWithFence, headingAnchors, wordCount, report, finish } from './lib.mjs';

const WORDS_PER_MINUTE = 100;
const minutes = words => Math.max(5, Math.round(words / WORDS_PER_MINUTE / 5) * 5);
const TAGS = '🎯|🔍|📖';

const dir = path.join(repoRoot, 'docs', 'training');
const modules = fs.readdirSync(dir).filter(f => /^[CTU]\d\d_.*\.md$/.test(f)).sort();

for (const name of modules) {
  const file = path.join(dir, name);
  const text = fs.readFileSync(file, 'utf8');
  const lines = linesWithFence(text);

  // Nhãn: dòng Mục lục "N. [..](#anchor) … 🎯" (nhãn ở cuối dòng), hoặc "[..](#anchor) 🎯" ở bất kỳ đâu.
  const tags = new Map();
  const addTag = (anchor, tag, no) => {
    anchor = decodeURIComponent(anchor);
    if (tags.has(anchor) && tags.get(anchor).tag !== tag) report(file, no, `mục #${anchor} có hai nhãn khác nhau`);
    tags.set(anchor, { tag, no });
  };
  for (const { line, no, inFence } of lines) {
    if (inFence) continue;
    const toc = new RegExp(`^\\d+\\. \\[[^\\]]*\\]\\(#([^)]+)\\).*?(${TAGS})\\s*$`).exec(line);
    if (toc) addTag(toc[1], toc[2], no);
    for (const m of line.matchAll(new RegExp(`\\]\\(#([^)]+)\\)\\s+(${TAGS})`, 'g'))) addTag(m[1], m[2], no);
  }

  // Chia trang theo mục "## "; phần trước mục đầu tiên là "đầu trang" (🎯).
  const sections = headingAnchors(text).filter(h => h.level === 2);
  const exempt = h => /^(Mục lục|✅)/.test(h.title);
  const untagged = tags.size === 0;
  const tagOf = new Map();
  for (const h of sections) {
    if (exempt(h)) { tagOf.set(h.no, '🎯'); continue; }
    if (untagged) { tagOf.set(h.no, '🎯'); continue; }
    if (!tags.has(h.anchor)) { report(file, h.no, `mục "${h.title}" chưa có nhãn 🎯/🔍/📖 ở Mục lục`); continue; }
    tagOf.set(h.no, tags.get(h.anchor).tag);
  }
  const sectionAnchors = new Set(sections.map(h => h.anchor));
  for (const [anchor, { no }] of tags) {
    if (!sectionAnchors.has(anchor)) report(file, no, `nhãn gắn cho #${anchor}, nhưng đó không phải một mục "## "`);
  }

  let coreWords = 0, current = '🎯';
  for (const { line, no } of lines) {
    if (tagOf.has(no)) current = tagOf.get(no);
    if (current === '🎯') coreWords += wordCount(line);
  }
  const all = minutes(wordCount(text)), core = minutes(coreWords);

  const cardLine = lines.find(l => !l.inFence && l.line.includes('**⏱ Đọc:**'));
  if (!cardLine) { report(file, 1, 'thiếu dòng "**⏱ Đọc:**" ở thẻ module'); continue; }
  const shownAll = /\*\*⏱ Đọc:\*\* ≈ (\d+) phút/.exec(cardLine.line);
  const shownCore = /phần 🎯 ≈ (\d+) phút/.exec(cardLine.line);
  if (!shownAll || +shownAll[1] !== all) {
    report(file, cardLine.no, `thời gian đọc cả trang phải là "≈ ${all} phút" (${wordCount(text)} từ)`);
  }
  if (untagged) {
    if (!cardLine.line.includes('mọi mục đều 🎯')) report(file, cardLine.no, 'module không gắn nhãn thì thẻ phải ghi "mọi mục đều 🎯"');
  } else if (!shownCore || +shownCore[1] !== core) {
    report(file, cardLine.no, `thời gian phần 🎯 phải là "phần 🎯 ≈ ${core} phút" (${coreWords} từ)`);
  }
}
finish(`${modules.length} module`);
