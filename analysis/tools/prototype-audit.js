#!/usr/bin/env node
'use strict';

// Prototype audit: structural invariants of the prototyping record under
// analysis/prototyping/. See analysis/prototyping/README.md.
//
// Modes:
//   default            Stage 7 gate: decision.md, screen-manifest.json, and
//                      the export catalog are mandatory; approval.md is only
//                      warned about.
//   --require-approval Stage 8/10/13/14 gate: additionally approval.md must
//                      exist and pin the manifest's exact export_set_version.
//
// Fail-closed: a missing manifest is an error unless the migration status
// file records an explicit owner waiver
// (owner_gates.prototyping_retroactive.status: waived).
//
// Parity-map coverage (rows <-> screens) remains the Stage 7 reviewer's
// checklist item until it is wired into this script.
//
// Env overrides (for tests): PROTOTYPE_DIR, MIGRATION_STATUS_FILE.

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PROTO_DIR = path.resolve(process.env.PROTOTYPE_DIR || path.join(__dirname, '..', 'prototyping'));
const STATUS_FILE = path.resolve(process.env.MIGRATION_STATUS_FILE || path.join(__dirname, '..', 'migration_status.yaml'));
const REQUIRE_APPROVAL = process.argv.includes('--require-approval');

const MANIFEST = path.join(PROTO_DIR, 'screen-manifest.json');
const WIREFRAMES = path.join(PROTO_DIR, 'wireframes');
const DECISION = path.join(PROTO_DIR, 'decision.md');
const APPROVAL = path.join(PROTO_DIR, 'approval.md');

const errors = [];
const warnings = [];

function fail(msg) { errors.push(msg); }
function warn(msg) { warnings.push(msg); }

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

function isNonEmptyString(v) { return typeof v === 'string' && v.trim().length > 0; }

const PLACEHOLDER = /<[a-z][^>\n]{0,80}>|YYYY-MM-DD/i;

function hasOwnerWaiver() {
  if (!fs.existsSync(STATUS_FILE)) return false;
  const lines = fs.readFileSync(STATUS_FILE, 'utf8').split(/\r?\n/);
  const start = lines.findIndex((l) => /^\s*prototyping_retroactive:\s*$/.test(l));
  if (start === -1) return false;
  const indent = lines[start].match(/^\s*/)[0].length;
  for (let i = start + 1; i < lines.length; i++) {
    const line = lines[i];
    if (line.trim() === '') continue;
    if (line.match(/^\s*/)[0].length <= indent) break;
    if (/^\s*status:\s*waived\s*$/.test(line)) return true;
  }
  return false;
}

function checkFilledDocument(file, label) {
  const body = fs.readFileSync(file, 'utf8');
  const m = body.match(PLACEHOLDER);
  if (m) fail(`${label}: template placeholder still present ("${m[0].slice(0, 40)}") — the document is not filled in`);
  return body;
}

function scanSecrets() {
  // Token-shaped values anywhere in the record, including text-based exports.
  const secretPattern = /(figd_[A-Za-z0-9_-]{10,}|AIza[0-9A-Za-z_-]{20,}|bearer\s+[A-Za-z0-9._-]{16,})/i;
  const textExt = new Set(['.md', '.json', '.txt', '.html', '.htm', '.svg', '.csv', '.xml', '.yaml', '.yml']);
  const walk = (dir) => fs.readdirSync(dir, { withFileTypes: true }).flatMap((e) => {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) return e.name === 'templates' ? [] : walk(p);
    return textExt.has(path.extname(e.name).toLowerCase()) ? [p] : [];
  });
  for (const f of walk(PROTO_DIR)) {
    const m = fs.readFileSync(f, 'utf8').match(secretPattern);
    if (m) {
      fail(`possible credential material in ${path.relative(PROTO_DIR, f)}: "${m[0].slice(0, 12)}..." — secrets never enter the record`);
    }
  }
}

function main() {
  if (!fs.existsSync(MANIFEST)) {
    if (hasOwnerWaiver()) {
      console.log('PROTOTYPE AUDIT SKIPPED: no screen-manifest.json, and the migration status records an explicit owner waiver (owner_gates.prototyping_retroactive: waived).');
      process.exit(0);
    }
    fail('screen-manifest.json does not exist and no owner waiver (owner_gates.prototyping_retroactive.status: waived) is recorded in the migration status — the prototype gate cannot be skipped silently');
    return report();
  }

  // Stage 5 record is a prerequisite of any manifest.
  if (!fs.existsSync(DECISION)) {
    fail('decision.md is missing: the Stage 5 form/style/palette decision must be recorded before wireframes exist');
  } else {
    checkFilledDocument(DECISION, 'decision.md');
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
  if (!isNonEmptyString(manifest.export_set_version) || PLACEHOLDER.test(manifest.export_set_version)) fail('export_set_version must be a non-empty, filled-in string');
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
      } else if (!isNonEmptyString(s.target_requirement) || PLACEHOLDER.test(s.target_requirement)) {
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
      if (!isNonEmptyString(f.sha256) || PLACEHOLDER.test(f.sha256)) { fail(`${flabel}: sha256 is required`); return; }
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

  // Approval: mandatory with --require-approval, otherwise a warning.
  if (fs.existsSync(APPROVAL)) {
    const approval = checkFilledDocument(APPROVAL, 'approval.md');
    if (!approval.includes(manifest.export_set_version)) {
      fail(`approval.md does not name the manifest export_set_version "${manifest.export_set_version}"`);
    }
    if (!/approved by/i.test(approval)) fail('approval.md must name the approver');
  } else if (REQUIRE_APPROVAL) {
    fail('approval.md is missing: the Stage 8 owner approval (with the approved export_set_version) is mandatory for this gate (--require-approval)');
  } else {
    warn('approval.md not present yet (required before Stage 8 closes; rerun with --require-approval)');
  }

  scanSecrets();
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
