// Hàm dùng chung cho các script kiểm tài liệu.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

/** Thư mục gốc của repo (tools/docs/ → ../..). */
export const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');

const skipDirs = new Set(['.git', 'bin', 'obj', '.vs', 'node_modules', 'coverage', 'TestResults', 'dist']);

// Thư mục mà template được phép xóa (`dotnet new cad-plugin --IncludeDemos false`, `init-template.ps1 -RemoveDemos`).
// Tài liệu vẫn trỏ vào đó; khi thư mục không còn, link/trích đoạn tới nó được bỏ qua thay vì làm đỏ CI.
const optionalDirs = ['Demos'];

/** true nếu `target` nằm trong một thư mục tùy chọn đã bị template xóa. */
export function removedByTemplate(target) {
  const first = path.relative(repoRoot, target).split(path.sep)[0];
  return optionalDirs.includes(first) && !fs.existsSync(path.join(repoRoot, first));
}

/** Mọi file .md trong repo, bỏ qua thư mục build và node_modules. */
export function markdownFiles(dir = repoRoot, out = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    if (skipDirs.has(e.name)) continue;
    const p = path.join(dir, e.name);
    if (e.isDirectory()) markdownFiles(p, out);
    else if (e.name.endsWith('.md')) out.push(p);
  }
  return out;
}

/** Các dòng của file kèm cờ "đang trong khối code" (dòng mở/đóng khối cũng tính là trong khối). */
export function linesWithFence(text) {
  let inFence = false;
  return text.split(/\r?\n/).map((line, i) => {
    const fence = /^\s*(```|~~~)/.test(line);
    if (fence) inFence = !inFence;
    return { line, no: i + 1, inFence: inFence || fence };
  });
}

/** Slug anchor theo cách GitHub (github-slugger). */
export function slug(text) {
  return text.toLowerCase()
    .replace(/[^\p{L}\p{M}\p{N}\p{Pc} -]/gu, '')
    .replace(/ /g, '-');
}

/** Chữ của heading như GitHub dùng để tạo anchor: giữ nguyên nội dung inline code, bỏ link/ảnh/HTML/dấu *. */
export function headingText(raw) {
  return raw.split('`').map((part, i) => i % 2 ? part : part
    .replace(/!\[[^\]]*\]\([^)]*\)/g, '')
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/<[^>]+>/g, '')
    .replace(/\*/g, '')).join('').trim();
}

/** Anchor của mọi heading trong một đoạn markdown, theo thứ tự, có hậu tố -1, -2 khi trùng. */
export function headingAnchors(text) {
  const counts = new Map();
  const result = [];
  for (const { line, no, inFence } of linesWithFence(text)) {
    if (inFence) continue;
    const m = /^(#{1,6})\s+(.*?)\s*#*\s*$/.exec(line);
    if (!m) continue;
    const s = slug(headingText(m[2]));
    const n = counts.get(s) || 0;
    counts.set(s, n + 1);
    result.push({ level: m[1].length, title: m[2], anchor: n ? `${s}-${n}` : s, no });
  }
  return result;
}

/** Số từ như `wc -w`: các chuỗi không chứa khoảng trắng, kể cả code. */
export function wordCount(text) {
  return text.split(/\s+/).filter(Boolean).length;
}

const errors = [];

/** Ghi một lỗi; trên GitHub Actions in thêm dạng annotation để hiện ngay trên dòng của PR. */
export function report(file, line, message) {
  const rel = path.relative(repoRoot, file).split(path.sep).join('/');
  errors.push(`${rel}:${line}: ${message}`);
  if (process.env.GITHUB_ACTIONS) console.log(`::error file=${rel},line=${line}::${message}`);
  else console.log(`${rel}:${line}: ${message}`);
}

/** In dòng tổng kết và đặt exit code 1 nếu có lỗi. */
export function finish(summary) {
  console.log(`${summary}, ${errors.length} lỗi`);
  if (errors.length) process.exitCode = 1;
}
