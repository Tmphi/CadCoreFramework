// Tổng hợp số đo hiệu quả học (docs/training/README §8) từ các issue tạo bằng hai mẫu
// .github/ISSUE_TEMPLATE/lab-report.yml ("Báo cáo lab") và stage-check.yml ("Đo đầu/cuối giai đoạn"):
//   - giờ làm lab thực tế so với ước lượng ở T10 §2;
//   - phút đọc thực tế so với dòng ⏱ của module;
//   - điểm trước/sau mỗi giai đoạn, trên đúng bộ câu vấn đáp ở T10 §4.
// Tên trường đọc từ chính hai file mẫu, ước lượng đọc từ chính tài liệu: không chép số liệu nào bằng tay.
// Chạy:  node tools/docs/learning-report.mjs                    (đọc issue của repo bằng `gh issue list`)
//        node tools/docs/learning-report.mjs --file issues.json  (mảng { number, title, body })
//        node tools/docs/learning-report.mjs --check             (CI: dữ liệu mẫu fixtures/ phải cho đúng báo cáo mẫu)
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { repoRoot, report, finish } from './lib.mjs';

const HOURS_PER_DAY = 8;          // "1 ngày" lab = 8 giờ làm việc
const MIN_REPORTS = 5;            // ít hơn thì chưa kết luận
const args = process.argv.slice(2);
const opt = name => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : null; };
const fixtureIn = path.join(repoRoot, 'tools', 'docs', 'fixtures', 'learning-issues.json');
const fixtureOut = path.join(repoRoot, 'tools', 'docs', 'fixtures', 'learning-report.md');

// ─── Tên trường của hai mẫu issue ──────────────────────────────────────────
function formLabels(file) {
  const labels = {};
  let id = null;
  for (const line of fs.readFileSync(file, 'utf8').split(/\r?\n/)) {
    const m = /^\s*id:\s*(\S+)/.exec(line);
    if (m) { id = m[1]; continue; }
    const l = /^\s*label:\s*(.+?)\s*$/.exec(line);
    if (l && id) { labels[id] = l[1].replace(/^"(.*)"$/, '$1'); id = null; }
  }
  return labels;
}
const templates = path.join(repoRoot, '.github', 'ISSUE_TEMPLATE');
const LAB = formLabels(path.join(templates, 'lab-report.yml'));
const STAGE = formLabels(path.join(templates, 'stage-check.yml'));

// ─── Số liệu trong tài liệu ────────────────────────────────────────────────
const training = path.join(repoRoot, 'docs', 'training');
const t10 = fs.readFileSync(path.join(training, 'T10_LABS_AND_ASSESSMENT.md'), 'utf8');

/** "3 giờ" → [3, 3]; "0.5–1 ngày" → [4, 8]. */
function hoursRange(text) {
  const m = /(\d+(?:\.\d+)?)(?:–(\d+(?:\.\d+)?))?\s*(giờ|ngày)/.exec(text);
  if (!m) return null;
  const k = m[3] === 'ngày' ? HOURS_PER_DAY : 1;
  return [+m[1] * k, +(m[2] ?? m[1]) * k];
}
const estimates = new Map();   // L02 → { text, range }
for (const line of t10.split(/\r?\n/)) {
  const m = /^\| (L\d\d) \| [^|]+ \| [^|]+ \| ([^|]+) \|/.exec(line);
  if (m) estimates.set(m[1], { text: m[2].trim(), range: hoursRange(m[2]) });
}
if (estimates.size === 0) report(path.join(training, 'T10_LABS_AND_ASSESSMENT.md'), 1, 'không đọc được bảng thời lượng lab ở T10 §2');

/** Bộ câu vấn đáp của từng lần lên cấp (T10 §4): "Q22–Q25" → Q22…Q25; Fresher → C1…C10 (C05 §6). */
const questionSets = new Map();
for (const line of t10.split(/\r?\n/)) {
  const m = /^\| (🌰 → 🌱 Fresher|🌱 → 🌿 Junior|🌿 → 🌳 Middle|🌳 → 🏔️ Senior) \|(?:[^|]*\|){2}([^|]*)\|/.exec(line);
  if (!m) continue;
  if (/C05/.test(m[2])) { questionSets.set(m[1], Array.from({ length: 10 }, (_, i) => `C${i + 1}`)); continue; }
  const ids = [];
  for (const r of m[2].matchAll(/Q(\d+)(?:–Q(\d+))?/g)) for (let q = +r[1]; q <= +(r[2] ?? r[1]); q++) ids.push(`Q${q}`);
  questionSets.set(m[1], ids);
}

/** Dòng ⏱ của module: { all, core } phút. */
function readingCard(code) {
  const file = fs.readdirSync(training).find(f => f.startsWith(code + '_'));
  if (!file) return null;
  const card = fs.readFileSync(path.join(training, file), 'utf8').split(/\r?\n/).find(l => l.includes('**⏱ Đọc:**')) ?? '';
  const all = /\*\*⏱ Đọc:\*\* ≈ (\d+) phút/.exec(card), core = /phần 🎯 ≈ (\d+) phút/.exec(card);
  return { all: all ? +all[1] : null, core: core ? +core[1] : (all ? +all[1] : null) };
}

// ─── Đọc issue ─────────────────────────────────────────────────────────────
/** Thân issue của mẫu form: "### Tên trường\n\ngiá trị". "_No response_" = bỏ trống. */
function fields(body) {
  const out = {};
  const parts = (body ?? '').replace(/\r\n/g, '\n').split(/^### /m).slice(1);
  for (const p of parts) {
    const nl = p.indexOf('\n');
    const label = (nl < 0 ? p : p.slice(0, nl)).trim();
    const value = (nl < 0 ? '' : p.slice(nl + 1)).trim();
    out[label] = value === '_No response_' ? '' : value.replace(/^```\w*\n?|\n?```$/g, '').trim();
  }
  return out;
}

const check = args.includes('--check');
let issues;
const file = check ? fixtureIn : opt('--file');
if (file) issues = JSON.parse(fs.readFileSync(file, 'utf8'));
else {
  try {
    issues = JSON.parse(execFileSync('gh', ['issue', 'list', '--state', 'all', '--limit', '1000', '--json', 'number,title,body'], { encoding: 'utf8' }));
  } catch (e) {
    console.error('Không chạy được `gh issue list` (cần GitHub CLI đã đăng nhập). Hoặc dùng --file <issues.json>.');
    process.exit(2);
  }
}

const warnings = [];
const labHours = new Map(), reading = new Map(), stages = new Map();
for (const issue of issues.slice().sort((a, b) => a.number - b.number)) {
  const f = fields(issue.body);
  if (LAB.lab in f) {
    const lab = f[LAB.lab], hours = Number(f[LAB.hours]);
    if (!estimates.has(lab)) { warnings.push(`#${issue.number}: lab "${lab}" không có trong T10 §2`); continue; }
    if (!(hours > 0)) { warnings.push(`#${issue.number}: thời gian "${f[LAB.hours]}" không phải số giờ`); continue; }
    labHours.set(lab, [...(labHours.get(lab) ?? []), hours]);
    for (const line of (f[LAB.reading] ?? '').split('\n')) {
      const m = /^\s*([CTU]\d\d)\s*(🎯)?\s*:\s*(\d+(?:\.\d+)?)\s*$/.exec(line);
      if (!m) { if (line.trim()) warnings.push(`#${issue.number}: dòng đọc "${line.trim()}" không đúng dạng "T01 🎯: 80"`); continue; }
      const key = `${m[1]}${m[2] ? ' 🎯' : ''}`;
      reading.set(key, [...(reading.get(key) ?? []), +m[3]]);
    }
  } else if (STAGE.stage in f) {
    const stage = f[STAGE.stage], when = f[STAGE.when], learner = f[STAGE.learner];
    const expected = questionSets.get(stage);
    if (!expected) { warnings.push(`#${issue.number}: cấp "${stage}" không có trong T10 §4`); continue; }
    const scores = new Map();
    for (const line of (f[STAGE.scores] ?? '').split('\n')) {
      const m = /^\s*([QC]\d+)\s*:\s*([012])\s*$/.exec(line);
      if (m) scores.set(m[1], +m[2]);
      else if (line.trim()) warnings.push(`#${issue.number}: dòng điểm "${line.trim()}" không đúng dạng "Q1: 2" (0–2)`);
    }
    const missing = expected.filter(q => !scores.has(q)), extra = [...scores.keys()].filter(q => !expected.includes(q));
    if (missing.length || extra.length) {
      warnings.push(`#${issue.number}: bộ câu của ${stage} là ${expected.join(', ')}` +
        (missing.length ? `; thiếu ${missing.join(', ')}` : '') + (extra.length ? `; thừa ${extra.join(', ')}` : ''));
      continue;
    }
    const percent = 100 * expected.reduce((s, q) => s + scores.get(q), 0) / (2 * expected.length);
    const key = `${stage}\u0000${learner}`;
    const entry = stages.get(key) ?? { stage, learner };
    entry[when === 'Trước khi học' ? 'pre' : 'post'] = percent;   // lần đo sau cùng thắng
    stages.set(key, entry);
  }
}

// ─── Báo cáo ───────────────────────────────────────────────────────────────
const median = xs => { const s = [...xs].sort((a, b) => a - b); const n = s.length; return n % 2 ? s[(n - 1) / 2] : (s[n / 2 - 1] + s[n / 2]) / 2; };
const num = x => (Math.round(x * 10) / 10).toString();
const out = [];
out.push('## Thời gian làm lab', '', '| Lab | Số báo cáo | Trung vị thực tế (giờ) | Ước lượng T10 §2 | Nhận xét |', '| :--- | ---: | ---: | :--- | :--- |');
for (const [lab, xs] of [...labHours].sort()) {
  const est = estimates.get(lab), m = median(xs), [lo, hi] = est.range ?? [NaN, NaN];
  let note = m < lo ? `nhanh hơn ước lượng ${Math.round(100 * (lo - m) / lo)}%` : m > hi ? `lâu hơn ước lượng ${Math.round(100 * (m - hi) / hi)}%` : 'trong khoảng ước lượng';
  if (xs.length < MIN_REPORTS) note += `; chưa đủ ${MIN_REPORTS} báo cáo để sửa T10 §2`;
  const inHours = lo === hi ? `${num(lo)} giờ` : `${num(lo)}–${num(hi)} giờ`;
  out.push(`| ${lab} | ${xs.length} | ${num(m)} | ${est.text}${est.text === inHours ? '' : ` (${inHours})`} | ${note} |`);
}
out.push('', '## Thời gian đọc', '', '| Module | Số báo cáo | Trung vị thực tế (phút) | ⏱ trên thẻ | Lệch |', '| :--- | ---: | ---: | ---: | ---: |');
for (const [key, xs] of [...reading].sort()) {
  const [code, core] = key.split(' ');
  const card = readingCard(code), shown = card ? (core ? card.core : card.all) : null;
  const m = median(xs);
  out.push(`| ${key} | ${xs.length} | ${num(m)} | ${shown ?? '?'} | ${shown ? (m >= shown ? '+' : '') + Math.round(100 * (m - shown) / shown) + '%' : '?'} |`);
}
out.push('', '## Tiến bộ theo giai đoạn', '', '| Lên cấp | Học viên có đủ hai lần đo | Trước khi học (trung bình) | Lúc lên cấp (trung bình) | Mức tăng |', '| :--- | ---: | ---: | ---: | ---: |');
const byStage = new Map();
for (const e of stages.values()) {
  if (e.pre === undefined || e.post === undefined) { warnings.push(`${e.learner} (${e.stage}): mới có một lần đo`); continue; }
  byStage.set(e.stage, [...(byStage.get(e.stage) ?? []), e]);
}
for (const stage of questionSets.keys()) {
  const es = byStage.get(stage);
  if (!es) continue;
  const pre = es.reduce((s, e) => s + e.pre, 0) / es.length, post = es.reduce((s, e) => s + e.post, 0) / es.length;
  out.push(`| ${stage} | ${es.length} | ${num(pre)}% | ${num(post)}% | ${post >= pre ? '+' : ''}${num(post - pre)} điểm % |`);
}
if (warnings.length) out.push('', '## Cần xem lại', '', ...warnings.map(w => `- ${w}`));
const text = out.join('\n') + '\n';

if (check) {
  const expected = fs.existsSync(fixtureOut) ? fs.readFileSync(fixtureOut, 'utf8').replace(/\r\n/g, '\n') : null;
  if (args.includes('--write')) fs.writeFileSync(fixtureOut, text);
  else if (expected !== text) {
    report(fixtureOut, 1, 'báo cáo từ dữ liệu mẫu khác báo cáo mẫu đã commit (T10 §2/§4, mẫu issue hay script vừa đổi?). ' +
      'Xem bằng "node tools/docs/learning-report.mjs --file tools/docs/fixtures/learning-issues.json"; đúng thì ghi lại bằng "--check --write"');
  }
  finish(`báo cáo mẫu: ${issues.length} issue, ${estimates.size} lab có ước lượng, ${questionSets.size} bộ câu lên cấp`);
} else {
  process.stdout.write(text);
}
