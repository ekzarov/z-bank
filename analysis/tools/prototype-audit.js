#!/usr/bin/env node
'use strict';

// Prototype audit (Stage 7 gate): structural invariants of the prototyping
// record under analysis/prototyping/. See analysis/prototyping/README.md.
// Parity-map coverage remains the Stage 7 reviewer's checklist item until it
// is wired into this script.

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PROTO_DIR = path.resolve(__dirname, '..', 'prototyping');
const MANIFEST = path.join(PROTO_DIR, 'screen-manifest.json');
const WIREFRAMES = path.join(PROTO_DIR, 'wireframes');
const APPROVAL = path.join(PROTO_DIR, 'approval.md');

const errors = [];
const warnings = [];

function fail(msg) { errors.push(msg); }
function warn(msg) { warnings.push(msg); }

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

function isNonEmptyString(v) { return typeof v === 'string' && v.trim().length > 0; }

function main() {
  if (!fs.existsSync(MANIFEST)) {
    // Scaffold-only state: nothing to audit yet. Distinguish from a broken run.
    console.log('PROTOTYPE AUDIT SKIPPED: analysis/prototyping/screen-manifest.json does not exist yet.');
    console.log('The audit becomes mandatory once Stage 6 produces a manifest.');
    process.exit(0);
  }

  let manifest;
  try {
    manifest = JSON.parse(fs.readFileSync(MANIFEST, 'utf8'));
  } catch (e) {
    fail(`screen-manifest.json is not valid JSON: ${e.message}`);
    return report();
  }

  if (manifest.schema_version !== 1) fail(`schema_version must be 1, got ${JSON.stringify(manifest.schema_version)}`);
  if (!isNonEmptyString(manifest.project) || manifest.project.startsWith('<')) fail('project must be filled in');
  if (!isNonEmptyString(manifest.tool)) fail('tool must name the wireframing tool (e.g. google-stitch, figma)');
  if (!isNonEmptyString(manifest.export_set_version)) fail('export_set_version must be a non-empty string');
  if (!Array.isArray(manifest.screens) || manifest.screens.length === 0) {
    fail('screens must be a non-empty array');
    return report();
  }

  const ids = new Set();
  const manifestFiles = new Set();

  manifest.screens.forEach((s, i) => {
    const label = `screens[${i}]${s && s.id ? ` (${s.id})` : ''}`;
    if (!isNonEmptyString(s.id)) fail(`${label}: id is required`);
    else if (ids.has(s.id)) fail(`${label}: duplicate id`);
    else ids.add(s.id);

    if (!isNonEmptyString(s.title)) fail(`${label}: title is required`);
    if (!isNonEmptyString(s.channel)) fail(`${label}: channel is required`);
    if (!Array.isArray(s.roles) || s.roles.length === 0) fail(`${label}: roles must be a non-empty array`);
    if (!Array.isArray(s.states) || s.states.length === 0) fail(`${label}: states must be a non-empty array`);

    const rows = s.workbook_rows;
    if (!Array.isArray(rows)) {
      fail(`${label}: workbook_rows must be an array`);
    } else if (rows.length === 0) {
      if (s.target_only !== true) {
        fail(`${label}: empty workbook_rows requires target_only: true`);
      } else if (!isNonEmptyString(s.target_requirement) || s.target_requirement.startsWith('<')) {
        fail(`${label}: target_only screens must reference an owner-approved target_requirement`);
      }
    } else if (!rows.every((r) => Number.isInteger(r) && r > 0)) {
      fail(`${label}: workbook_rows must contain positive integers`);
    }

    if (!Array.isArray(s.files) || s.files.length === 0) {
      fail(`${label}: files must be a non-empty array`);
      return;
    }
    s.files.forEach((f, j) => {
      const flabel = `${label}.files[${j}]`;
      if (!isNonEmptyString(f.path)) { fail(`${flabel}: path is required`); return; }
      if (f.path.includes('..') || path.isAbsolute(f.path)) { fail(`${flabel}: path must be relative inside analysis/prototyping/`); return; }
      manifestFiles.add(path.normalize(f.path));
      const abs = path.join(PROTO_DIR, f.path);
      if (!fs.existsSync(abs)) { fail(`${flabel}: file not found: ${f.path}`); return; }
      if (!isNonEmptyString(f.sha256) || f.sha256.startsWith('<')) { fail(`${flabel}: sha256 is required`); return; }
      const actual = sha256(abs);
      if (actual.toLowerCase() !== f.sha256.toLowerCase()) {
        fail(`${flabel}: sha256 mismatch (manifest ${f.sha256}, actual ${actual})`);
      }
    });
  });

  // Every exported file must be claimed by the manifest.
  if (fs.existsSync(WIREFRAMES)) {
    const walk = (dir) => fs.readdirSync(dir, { withFileTypes: true }).flatMap((e) => {
      const p = path.join(dir, e.name);
      return e.isDirectory() ? walk(p) : [p];
    });
    for (const abs of walk(WIREFRAMES)) {
      const rel = path.normalize(path.relative(PROTO_DIR, abs));
      if (!manifestFiles.has(rel)) fail(`unmanifested export: ${rel} exists in wireframes/ but no screen lists it`);
    }
  } else {
    fail('wireframes/ directory does not exist although a manifest is present');
  }

  // Approval, when present, must pin the exact export set.
  if (fs.existsSync(APPROVAL)) {
    const approval = fs.readFileSync(APPROVAL, 'utf8');
    if (!approval.includes(manifest.export_set_version)) {
      fail(`approval.md does not name the manifest export_set_version "${manifest.export_set_version}"`);
    }
    if (!/approved by/i.test(approval)) fail('approval.md must name the approver');
  } else {
    warn('approval.md not present yet (required before Stage 8 closes)');
  }

  // No credential material anywhere in the record. Mentioning "API key" as a
  // word is fine; token-shaped values are not.
  const secretPattern = /(figd_[A-Za-z0-9_-]{10,}|AIza[0-9A-Za-z_-]{20,}|bearer\s+[A-Za-z0-9._-]{16,})/i;
  const textFiles = [MANIFEST, APPROVAL, path.join(PROTO_DIR, 'decision.md')].filter((f) => fs.existsSync(f));
  for (const f of textFiles) {
    const m = fs.readFileSync(f, 'utf8').match(secretPattern);
    if (m) {
      fail(`possible credential material in ${path.relative(PROTO_DIR, f)}: "${m[0].slice(0, 12)}..." — secrets never enter the record`);
    }
  }

  report();
}

function report() {
  for (const w of warnings) console.log(`WARN: ${w}`);
  if (errors.length) {
    for (const e of errors) console.error(`FAIL: ${e}`);
    console.error(`PROTOTYPE AUDIT FAILED: ${errors.length} error(s).`);
    process.exit(1);
  }
  console.log('PROTOTYPE AUDIT OK');
}

main();
