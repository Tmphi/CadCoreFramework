// Kiểm chính bộ bài tập chấm tự động trong exercises/ (docs/training/README.md §7):
//   1. Với lời giải mẫu (exercises/LoiGiai, -p:UseSolutions=true): build không cảnh báo, MỌI test pass.
//   2. Với code khởi đầu (exercises/Exercises): build không cảnh báo, và MỖI bài có ít nhất một test đỏ
//      — nếu không, test của bài đó không kiểm được gì.
//   3. Mỗi file bài làm Exercises/<Module>/E<n>.cs có đúng một lớp test Exercises.Tests.<Module>.E<n>Tests.
// Chạy: node tools/docs/check-exercises.mjs   (cần .NET SDK 8 trở lên)
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { repoRoot, report, finish } from './lib.mjs';

const dir = path.join(repoRoot, 'exercises');
const sln = path.join(dir, 'Exercises.sln');
const results = path.join(os.tmpdir(), 'cadcore-exercises-results');
fs.rmSync(results, { recursive: true, force: true });

const dotnet = args => spawnSync('dotnet', args, {
  cwd: dir, encoding: 'utf8', timeout: 900000,
  env: { ...process.env, DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1' },
});
const tail = r => (r.stdout + r.stderr).split(/\r?\n/).filter(l => /error|Failed|Passed!/.test(l)).slice(0, 3).join(' | ');

// Đọc TRX: lớp test → số test đỏ / tổng.
function readTrx(file) {
  const xml = fs.readFileSync(file, 'utf8');
  const classOf = new Map();
  for (const m of xml.matchAll(/<UnitTest [^>]*id="([^"]+)"[\s\S]*?<TestMethod [^>]*className="([^"]+)"/g)) classOf.set(m[1], m[2].split(',')[0]);
  const byClass = new Map();
  for (const m of xml.matchAll(/<UnitTestResult [^>]*testId="([^"]+)"[^>]*outcome="([^"]+)"/g)) {
    const cls = classOf.get(m[1]) ?? '?';
    const s = byClass.get(cls) ?? { failed: 0, total: 0 };
    s.total++;
    if (m[2] !== 'Passed') s.failed++;
    byClass.set(cls, s);
  }
  return byClass;
}

function run(mode, extra) {
  const build = dotnet(['build', sln, '-warnaserror', '-v:q', '-nologo', ...extra]);
  if (build.status !== 0) { report(sln, 1, `${mode}: build lỗi hoặc có cảnh báo: ${tail(build)}`); return null; }
  const trx = `${mode}.trx`;
  dotnet(['test', sln, '--no-build', '-v:q', '-nologo', ...extra, '--logger', `trx;LogFileName=${trx}`, '--results-directory', results]);
  const file = path.join(results, trx);
  if (!fs.existsSync(file)) { report(sln, 1, `${mode}: không có kết quả test`); return null; }
  return readTrx(file);
}

// 1. Lời giải mẫu: mọi test pass. (Chạy trước, build lại khi đổi chế độ là nhờ UseSolutions đổi Compile.)
const solved = run('loi-giai', ['-p:UseSolutions=true']);
if (solved) {
  for (const [cls, s] of solved) {
    if (s.failed) report(sln, 1, `lời giải mẫu không qua ${s.failed}/${s.total} test của ${cls} — sửa test hoặc exercises/LoiGiai`);
  }
}

// 2. Code khởi đầu: mỗi bài có ít nhất một test đỏ. Build lại sạch để không dùng nhầm bản lời giải.
dotnet(['clean', sln, '-v:q', '-nologo']);
const starter = run('bai-lam', []);
if (starter) {
  for (const [cls, s] of starter) {
    if (s.failed === 0) report(sln, 1, `${cls}: cả ${s.total} test đều xanh với code khởi đầu — test không kiểm được bài làm`);
  }
}

// 3. File bài làm ↔ lớp test.
const testClasses = new Set([...(starter ?? solved ?? new Map()).keys()]);
const exerciseFiles = [];
for (const module of fs.readdirSync(path.join(dir, 'Exercises'), { withFileTypes: true }).filter(e => e.isDirectory() && !['bin', 'obj'].includes(e.name))) {
  for (const f of fs.readdirSync(path.join(dir, 'Exercises', module.name)).filter(f => /^E\d+\.cs$/.test(f))) {
    const cls = `Exercises.Tests.${module.name}.${f.replace('.cs', '')}Tests`;
    exerciseFiles.push(cls);
    if (!testClasses.has(cls)) report(path.join(dir, 'Exercises', module.name, f), 1, `không có lớp test ${cls}`);
  }
}
for (const cls of testClasses) {
  if (!exerciseFiles.includes(cls)) report(sln, 1, `lớp test ${cls} không ứng với file bài làm nào trong exercises/Exercises`);
}

const total = solved ? [...solved.values()].reduce((a, s) => a + s.total, 0) : 0;
finish(`${exerciseFiles.length} bài, ${total} test`);
