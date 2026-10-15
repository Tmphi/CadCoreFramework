// Biên dịch và chạy code mẫu C# trong tài liệu, so output với khối ```text đi kèm.
//
// Mỗi khối ```csharp trong các module bắt buộc (REQUIRED) phải có một dòng chú thích ngay trước nó
// (dòng không trống gần nhất), GitHub không hiển thị dòng này:
//   <!-- doc-check: run -->                  biên dịch + chạy, phải thoát với mã 0
//   <!-- doc-check: run output -->           như trên, và stdout phải trùng khối ```text ngay sau đó
//   <!-- doc-check: compile -->              chỉ biên dịch
//   <!-- doc-check: compile-error CS0029 --> biên dịch phải lỗi với đúng mã đó
//   <!-- doc-check: test -->                 file test xUnit + FluentAssertions; `dotnet test` phải pass
//   <!-- doc-check: verbatim Core/X.cs -->   trích code dự án: bỏ comment `//` và dòng trống ở cả hai bên,
//                                            mọi dòng của khối phải có trong file đó, đúng thứ tự
//   <!-- doc-check: skip <lý do> -->         không kiểm (mã giả có "…", test cần lời giải của học viên…)
// Thêm sau loại: `allow CS8602` cho phép một cảnh báo (mặc định cảnh báo là lỗi), `pkg Tên.Package`
// thêm một NuGet package (version lấy từ csproj của repo để khỏi lệch).
//
// Mỗi khối thành một project giống `dotnet new console -f net8.0` (ImplicitUsings, Nullable bật) cộng
// <LangVersion>10.0</LangVersion>, như người học được dặn ở C00/C01. Culture cố định là InvariantCulture
// để output giống tài liệu (dấu chấm thập phân) trên mọi máy.
// Chạy: node tools/docs/check-snippets.mjs [lọc-tên-file] [--assume-run]   (cần .NET SDK 8 trở lên)
// --assume-run: khối chưa có chú thích được coi là "run" (hoặc "run output" nếu ngay sau là khối text),
// để thử nhanh khi viết bài trước khi gắn chú thích.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { repoRoot, linesWithFence, removedByTemplate, report, finish } from './lib.mjs';

const REQUIRED = /^(C0[0-5]|U01)_.*\.md$/;
const assumeRun = process.argv.includes('--assume-run');
const filter = process.argv.slice(2).find(a => !a.startsWith('--')) || '';
const work = path.join(os.tmpdir(), 'cadcore-doc-snippets');
const docsDir = path.join(repoRoot, 'docs', 'training');

// Version package lấy từ chính các csproj của repo.
const versions = {};
for (const csproj of ['CadCoreFramework.csproj', 'CadCoreFramework.Tests/CadCoreFramework.Tests.csproj']) {
  const xml = fs.readFileSync(path.join(repoRoot, csproj), 'utf8');
  for (const m of xml.matchAll(/<PackageReference\s+Include="([^"]+)"\s+Version="([^"]+)"/g)) versions[m[1]] = m[2];
}
const pkg = name => {
  if (!versions[name]) throw new Error(`không thấy version của ${name} trong csproj của repo`);
  return `    <PackageReference Include="${name}" Version="${versions[name]}" />`;
};

// ---- 1. Đọc các khối và chú thích ----
const blocks = [];
for (const name of fs.readdirSync(docsDir).filter(f => f.endsWith('.md') && f.includes(filter)).sort()) {
  const file = path.join(docsDir, name);
  const lines = linesWithFence(fs.readFileSync(file, 'utf8'));
  for (let i = 0; i < lines.length; i++) {
    if (!/^```csharp\s*$/.test(lines[i].line)) continue;
    const start = lines[i].no;
    let p = i - 1;
    while (p >= 0 && lines[p].line.trim() === '') p--;
    let note = p >= 0 ? /^<!-- doc-check: (.+?) -->$/.exec(lines[p].line.trim()) : null;
    const body = [];
    for (i++; i < lines.length && !/^```\s*$/.test(lines[i].line); i++) body.push(lines[i].line);
    // Khối code kế tiếp (để lấy output ```text).
    let next = null;
    for (let j = i + 1; j < lines.length; j++) {
      if (/^```/.test(lines[j].line)) {
        if (/^```text\s*$/.test(lines[j].line)) {
          const out = [];
          for (j++; j < lines.length && !/^```\s*$/.test(lines[j].line); j++) out.push(lines[j].line);
          next = out;
        }
        break;
      }
    }
    if (!note && assumeRun) note = [null, next ? 'run output' : 'run'];
    if (!note) {
      if (REQUIRED.test(name)) report(file, start, 'khối csharp thiếu dòng <!-- doc-check: … --> ngay trước nó');
      continue;
    }
    const words = note[1].split(/\s+/);
    const block = { file, name, start, body, next, kind: words[0], args: words.slice(1), allow: [], pkgs: [] };
    for (let k = 1; k < words.length; k++) {
      if (words[k] === 'allow') block.allow.push(words[++k]);
      if (words[k] === 'pkg') block.pkgs.push(words[++k]);
    }
    if (!['run', 'compile', 'compile-error', 'test', 'verbatim', 'skip'].includes(block.kind)) {
      report(file, start, `doc-check không hiểu loại "${block.kind}"`);
      continue;
    }
    if (block.kind === 'skip' && block.args.length === 0) report(file, start, 'doc-check: skip phải ghi lý do');
    if (block.kind === 'run' && block.args[0] === 'output' && !next) report(file, start, 'run output nhưng ngay sau khối không có khối ```text');
    if (block.kind === 'compile-error' && !/^CS\d{4}$/.test(block.args[0] || '')) report(file, start, 'compile-error phải ghi mã lỗi, ví dụ CS0029');
    blocks.push(block);
  }
}

// ---- 2. Trích nguyên văn: so với file nguồn, không cần build ----
const codeLines = text => text.split(/\r?\n/).map(l => l.replace(/(^|\s)\/\/.*$/, '').trim()).filter(Boolean);
for (const b of blocks.filter(b => b.kind === 'verbatim')) {
  const src = path.join(repoRoot, b.args[0] || '');
  if (b.args[0] && removedByTemplate(src)) continue;
  if (!b.args[0] || !fs.existsSync(src)) { report(b.file, b.start, `verbatim: không có file "${b.args[0] ?? ''}"`); continue; }
  const want = codeLines(b.body.join('\n')), have = codeLines(fs.readFileSync(src, 'utf8'));
  let k = 0;
  for (const l of have) if (l === want[k]) k++;
  if (k < want.length) report(b.file, b.start, `verbatim: dòng "${want[k]}" không có trong ${b.args[0]} (hoặc sai thứ tự)`);
}

// ---- 3. Sinh project ----
fs.rmSync(work, { recursive: true, force: true });
fs.mkdirSync(work, { recursive: true });
fs.writeFileSync(path.join(work, 'Directory.Build.props'), `<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>10.0</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
  </PropertyGroup>
</Project>
`);
// Culture + encoding cố định, chạy trước mọi dòng của khối.
const docSetup = `using System.Globalization;
using System.Runtime.CompilerServices;

static class DocCheckSetup
{
    [ModuleInitializer]
    internal static void Init()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Console.OutputEncoding = System.Text.Encoding.UTF8;
    }
}
`;
const checkedBlocks = blocks.filter(b => b.kind !== 'skip' && b.kind !== 'verbatim');
for (const b of checkedBlocks) {
  b.id = `${b.name.slice(0, 3)}_L${b.start}`;
  b.dir = path.join(work, b.id);
  fs.mkdirSync(b.dir);
  const test = b.kind === 'test';
  const refs = (test
    ? ['Microsoft.NET.Test.Sdk', 'xunit', 'xunit.runner.visualstudio', 'FluentAssertions'].map(pkg)
    : []).concat(b.pkgs.map(pkg));
  fs.writeFileSync(path.join(b.dir, `${b.id}.csproj`), `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    ${test ? '<IsPackable>false</IsPackable>' : b.kind === 'compile' ? '<OutputType>Library</OutputType>' : '<OutputType>Exe</OutputType>'}
    <NoWarn>$(NoWarn);${b.allow.join(';')}</NoWarn>
  </PropertyGroup>
  <ItemGroup>
${refs.join('\n')}
  </ItemGroup>
</Project>
`);
  fs.writeFileSync(path.join(b.dir, test ? 'DocTests.cs' : 'Program.cs'), b.body.join('\n') + '\n');
  if (b.kind === 'run') fs.writeFileSync(path.join(b.dir, 'DocCheckSetup.cs'), docSetup);
}

const dotnet = (args, cwd = work, timeout = 600000) =>
  spawnSync('dotnet', args, { cwd, encoding: 'utf8', timeout, env: { ...process.env, DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1' } });
const firstErrors = text => [...new Set(text.split(/\r?\n/).filter(l => /error [A-Z]+\d+/.test(l))
  .map(l => l.replace(/^.*?(\w+\.cs\(\d+,\d+\)): /, '$1: ').replace(/\s*\[[^\]]*\]\s*$/, '')))].slice(0, 3).join(' | ');

// ---- 3. Build song song các khối phải biên dịch được ----
const mustBuild = checkedBlocks.filter(b => b.kind !== 'compile-error');
fs.writeFileSync(path.join(work, 'all.proj'), `<Project DefaultTargets="Build">
  <ItemGroup>
${mustBuild.map(b => `    <P Include="${b.id}/${b.id}.csproj" />`).join('\n')}
  </ItemGroup>
  <Target Name="Restore">
    <MSBuild Projects="@(P)" Targets="Restore" Properties="MSBuildRestoreSessionId=$([System.Guid]::NewGuid())" BuildInParallel="true" />
  </Target>
  <Target Name="Build">
    <MSBuild Projects="@(P)" Targets="Build" BuildInParallel="true" ContinueOnError="true" />
  </Target>
</Project>
`);
console.log(`Build ${mustBuild.length} khối…`);
const all = dotnet(['build', 'all.proj', '-m', '-v:q', '-nologo', '-clp:NoSummary']);
for (const b of mustBuild) {
  const dll = path.join(b.dir, 'bin', 'Debug', 'net8.0', `${b.id}.dll`);
  if (fs.existsSync(dll)) { b.dll = dll; continue; }
  // Build lẻ để lấy đúng thông báo lỗi của khối này.
  const one = dotnet(['build', `${b.id}.csproj`, '-v:q', '-nologo', '-clp:NoSummary'], b.dir);
  if (one.status === 0 && fs.existsSync(dll)) { b.dll = dll; continue; }
  report(b.file, b.start, `khối không biên dịch được: ${firstErrors(one.stdout + one.stderr) || 'xem log dotnet build'}`);
}

// ---- 4. compile-error: phải lỗi đúng mã ----
for (const b of checkedBlocks.filter(b => b.kind === 'compile-error')) {
  const r = dotnet(['build', `${b.id}.csproj`, '-v:q', '-nologo', '-clp:NoSummary'], b.dir);
  const code = b.args[0];
  if (r.status === 0) report(b.file, b.start, `mong lỗi ${code} nhưng khối biên dịch được`);
  else if (!new RegExp(`error ${code}\\b`).test(r.stdout + r.stderr)) {
    report(b.file, b.start, `mong lỗi ${code}, nhận: ${firstErrors(r.stdout + r.stderr)}`);
  }
}

// ---- 5. Chạy và so output ----
const normalize = lines => {
  const out = lines.map(l => l.replace(/\s+$/, ''));
  while (out.length && out[out.length - 1] === '') out.pop();
  return out;
};
for (const b of checkedBlocks.filter(b => b.dll && b.kind === 'run')) {
  const r = dotnet([b.dll], b.dir, 30000);
  if (r.error || r.status !== 0) {
    report(b.file, b.start, `khối chạy lỗi (exit ${r.status ?? 'timeout'}): ${(r.stderr || String(r.error)).split(/\r?\n/).slice(0, 2).join(' ')}`);
    continue;
  }
  if (b.args[0] !== 'output') continue;
  const got = normalize(r.stdout.split(/\r?\n/)), want = normalize(b.next);
  const i = got.findIndex((l, k) => l !== want[k]);
  if (i >= 0 || got.length !== want.length) {
    const k = i >= 0 ? i : Math.min(got.length, want.length);
    report(b.file, b.start, `output khác khối text ở dòng ${k + 1}: tài liệu "${want[k] ?? '(hết)'}", chạy ra "${got[k] ?? '(hết)'}"`);
  }
}

// ---- 6. Test ----
for (const b of checkedBlocks.filter(b => b.dll && b.kind === 'test')) {
  const r = dotnet(['test', `${b.id}.csproj`, '--no-build', '-v:q', '-nologo'], b.dir);
  if (r.status !== 0) report(b.file, b.start, `test không pass: ${(r.stdout.split(/\r?\n/).find(l => /Failed|error/.test(l)) || '').trim()}`);
}

const count = k => blocks.filter(b => b.kind === k).length;
finish(`${blocks.length} khối (run ${count('run')}, compile ${count('compile')}, compile-error ${count('compile-error')}, test ${count('test')}, verbatim ${count('verbatim')}, skip ${count('skip')})`);
