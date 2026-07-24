#!/usr/bin/env node
'use strict';

// Regression tests for prototype-audit.js. Each case runs the audit as a
// child process against a disposable fixture directory.

const { spawnSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');

const AUDIT = path.join(__dirname, 'prototype-audit.js');
let failures = 0;
let caseNo = 0;

function check(name, cond, detail) {
  caseNo += 1;
  if (cond) {
    console.log(`ok ${caseNo} - ${name}`);
  } else {
    failures += 1;
    console.error(`not ok ${caseNo} - ${name}${detail ? `: ${detail}` : ''}`);
  }
}

function tmpdir() {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'proto-audit-'));
}

function writeStatus(dir, waived) {
  const p = path.join(dir, 'migration_status.yaml');
  fs.writeFileSync(p, [
    'owner_gates:',
    '  prototyping_retroactive:',
    `    status: ${waived ? 'waived' : 'pending'}`,
    '    approved_by: project owner',
    '',
  ].join('\n'));
  return p;
}

function run(protoDir, statusFile, args = []) {
  return spawnSync(process.execPath, [AUDIT, ...args], {
    encoding: 'utf8',
    env: { ...process.env, PROTOTYPE_DIR: protoDir, MIGRATION_STATUS_FILE: statusFile },
  });
}

function sha256(buf) {
  return crypto.createHash('sha256').update(buf).digest('hex');
}

function writeValidRecord(dir, opts = {}) {
  fs.mkdirSync(path.join(dir, 'wireframes'), { recursive: true });
  fs.writeFileSync(path.join(dir, 'decision.md'), [
    '# Stage 5 Decision',
    '- Date: 2026-07-24',
    '- Approved by: project owner',
    '- Channels: web',
    '- Direction: minimalism',
    '- Palette: slate/amber, dark and light',
    '',
  ].join('\n'));
  const png = Buffer.from('fake-png-bytes');
  fs.writeFileSync(path.join(dir, 'wireframes', 'login.png'), png);
  const manifest = {
    schema_version: 1,
    project: 'fixture',
    tool: 'figma',
    export_set_version: '2026-07-24-01',
    decision: 'decision.md',
    screens: [{
      id: 'login',
      title: 'Login',
      channel: 'web',
      roles: ['customer'],
      states: ['default', 'error'],
      workbook_rows: [1, 2],
      files: [{ path: 'wireframes/login.png', sha256: opts.badHash ? 'deadbeef' : sha256(png) }],
    }],
  };
  fs.writeFileSync(path.join(dir, 'screen-manifest.json'), JSON.stringify(manifest, null, 2));
  if (opts.approval) {
    fs.writeFileSync(path.join(dir, 'approval.md'), [
      '# Stage 8 Approval',
      '- Approved by: project owner',
      `- Approved export set: export_set_version: ${opts.approvalVersion || '2026-07-24-01'}`,
      '',
    ].join('\n'));
  }
  return dir;
}

// 1. Missing manifest + recorded waiver -> exit 0, SKIPPED.
{
  const d = tmpdir();
  const r = run(d, writeStatus(d, true));
  check('missing manifest with waiver skips with exit 0', r.status === 0 && r.stdout.includes('SKIPPED'), r.stdout + r.stderr);
}

// 2. Missing manifest, no waiver -> fail closed.
{
  const d = tmpdir();
  const r = run(d, writeStatus(d, false));
  check('missing manifest without waiver fails', r.status === 1 && /cannot be skipped silently/.test(r.stderr), r.stdout + r.stderr);
}

// 3. Missing manifest, no status file at all -> fail closed.
{
  const d = tmpdir();
  const r = run(d, path.join(d, 'no-such-status.yaml'));
  check('missing manifest without status file fails', r.status === 1, r.stdout + r.stderr);
}

// 4. Valid record (no approval, default mode) -> OK with warning.
{
  const d = writeValidRecord(tmpdir());
  const r = run(d, writeStatus(d, false));
  check('valid record passes default mode', r.status === 0 && r.stdout.includes('PROTOTYPE AUDIT OK'), r.stdout + r.stderr);
  check('missing approval only warns in default mode', r.stdout.includes('WARN') && /approval\.md not present/.test(r.stdout), r.stdout);
}

// 5. Valid record without approval fails --require-approval.
{
  const d = writeValidRecord(tmpdir());
  const r = run(d, writeStatus(d, false), ['--require-approval']);
  check('missing approval fails --require-approval', r.status === 1 && /approval\.md is missing/.test(r.stderr), r.stdout + r.stderr);
}

// 6. Valid record with matching approval passes --require-approval.
{
  const d = writeValidRecord(tmpdir(), { approval: true });
  const r = run(d, writeStatus(d, false), ['--require-approval']);
  check('matching approval passes --require-approval', r.status === 0 && r.stdout.includes('PROTOTYPE AUDIT OK'), r.stdout + r.stderr);
}

// 7. Approval pinning the wrong export set fails.
{
  const d = writeValidRecord(tmpdir(), { approval: true, approvalVersion: '1999-01-01-99' });
  const r = run(d, writeStatus(d, false), ['--require-approval']);
  check('approval with wrong export_set_version fails', r.status === 1 && /does not name the manifest export_set_version/.test(r.stderr), r.stdout + r.stderr);
}

// 8. Missing decision.md fails.
{
  const d = writeValidRecord(tmpdir());
  fs.rmSync(path.join(d, 'decision.md'));
  const r = run(d, writeStatus(d, false));
  check('missing decision.md fails', r.status === 1 && /decision\.md is missing/.test(r.stderr), r.stdout + r.stderr);
}

// 9. Unfilled decision template fails.
{
  const d = writeValidRecord(tmpdir());
  fs.writeFileSync(path.join(d, 'decision.md'), '# Stage 5 Decision\n- Date: YYYY-MM-DD\n- Channels: <web | mobile>\n');
  const r = run(d, writeStatus(d, false));
  check('placeholder decision fails', r.status === 1 && /template placeholder still present/.test(r.stderr), r.stdout + r.stderr);
}

// 10. Hash mismatch fails.
{
  const d = writeValidRecord(tmpdir(), { badHash: true });
  const r = run(d, writeStatus(d, false));
  check('sha256 mismatch fails', r.status === 1 && /sha256 mismatch/.test(r.stderr), r.stdout + r.stderr);
}

// 11. Unmanifested export fails.
{
  const d = writeValidRecord(tmpdir());
  fs.writeFileSync(path.join(d, 'wireframes', 'orphan.png'), 'x');
  const r = run(d, writeStatus(d, false));
  check('unmanifested export fails', r.status === 1 && /unmanifested export/.test(r.stderr), r.stdout + r.stderr);
}

// 12. Token-shaped secret inside a text export fails.
{
  const d = writeValidRecord(tmpdir());
  const svg = Buffer.from('<svg><!-- figd_0123456789abcdef --></svg>');
  fs.writeFileSync(path.join(d, 'wireframes', 'leak.svg'), svg);
  const manifest = JSON.parse(fs.readFileSync(path.join(d, 'screen-manifest.json'), 'utf8'));
  manifest.screens[0].files.push({ path: 'wireframes/leak.svg', sha256: sha256(svg) });
  fs.writeFileSync(path.join(d, 'screen-manifest.json'), JSON.stringify(manifest, null, 2));
  const r = run(d, writeStatus(d, false));
  check('secret in text export fails', r.status === 1 && /credential material/.test(r.stderr), r.stdout + r.stderr);
}

console.log(failures ? `${failures} test(s) failed` : 'all prototype-audit tests passed');
process.exit(failures ? 1 : 0);
