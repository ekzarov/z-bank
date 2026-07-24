'use strict';

const fs = require('fs');
const path = require('path');
const ExcelJS = require('exceljs');
const { cellText, parseRefs } = require('./lib');

const root = path.resolve(__dirname, '..', '..');
const coverageFile = path.join(
  root,
  'analysis',
  'stages',
  'stage-05',
  'stage-05-sdd-coverage.json'
);
const workbookFile = path.join(root, 'analysis', 'legacy_user_flows.xlsx');
const traceabilityFile = path.join(root, 'specs', 'traceability.md');
const migrationStatusFile = path.join(root, 'analysis', 'migration_status.yaml');

function implementationHasStarted(status) {
  const section = status.match(/^methodology_stage:\s*\r?\n([\s\S]*?)(?=^\S|\Z)/m)?.[1] || '';
  const number = Number(section.match(/^\s+number:\s*(\d+)\s*$/m)?.[1]);
  const lastCompleted = Number(section.match(/^\s+last_completed_stage:\s*(\d+)\s*$/m)?.[1]);
  if (!Number.isInteger(number)) throw new Error('Cannot read methodology_stage.number from migration_status.yaml');
  if (!Number.isInteger(lastCompleted)) throw new Error('Cannot read methodology_stage.last_completed_stage from migration_status.yaml');
  return number >= 7 || lastCompleted >= 7;
}

async function run() {
  const status = fs.readFileSync(migrationStatusFile, 'utf8');
  const implementationStarted = implementationHasStarted(status);
  const coverage = JSON.parse(fs.readFileSync(coverageFile, 'utf8'));
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(workbookFile);
  const sheet = workbook.getWorksheet('User Flows');
  if (!sheet) throw new Error('User Flows sheet not found');

  const expectedRows = [];
  for (let rowNumber = 7; rowNumber <= sheet.rowCount; rowNumber++) {
    if (!/^UF-\d+/.test(cellText(sheet.getRow(rowNumber).getCell(1)))) {
      expectedRows.push(rowNumber);
    }
  }

  const ownership = new Map();
  const errors = [];
  const traceability = fs.readFileSync(traceabilityFile, 'utf8');
  const qualifiedRequirements = new Set();
  let artifactCount = 0;
  let requirementCount = 0;
  const decisionRows = new Map();
  for (const [references, note] of Object.entries(coverage.decisionNotes || {})) {
    for (const decision of note.matchAll(/D-\d{3}/g)) {
      if (!decisionRows.has(decision[0])) decisionRows.set(decision[0], new Set());
      parseRefs(references).forEach((row) => decisionRows.get(decision[0]).add(row));
    }
  }
  for (const match of traceability.matchAll(/Feature\s+(\d{3})\s+([^;|\r\n]+)/g)) {
    const featureId = match[1];
    const references = match[2];
    for (const requirement of references.matchAll(/FR-(\d+)([A-Z]?)/g)) {
      qualifiedRequirements.add(`${featureId}:FR-${requirement[1]}${requirement[2]}`);
    }
    for (const range of references.matchAll(/FR-(\d+)\s+through\s+FR-(\d+)/g)) {
      for (let n = Number(range[1]); n <= Number(range[2]); n += 1) {
        qualifiedRequirements.add(`${featureId}:FR-${String(n).padStart(3, '0')}`);
      }
    }
  }
  for (const slice of coverage.slices) {
    const directory = path.join(root, 'specs', `${slice.id}-${slice.slug}`);
    for (const artifact of ['spec.md', 'plan.md', 'tasks.md']) {
      const file = path.join(directory, artifact);
      if (!fs.existsSync(file)) errors.push(`Missing ${path.relative(root, file)}`);
      else artifactCount += 1;
    }

    const specFile = path.join(directory, 'spec.md');
    if (fs.existsSync(specFile)) {
      const spec = fs.readFileSync(specFile, 'utf8');
      const requirementIds = [...spec.matchAll(/\*\*(FR-[0-9]+[A-Z]?)\*\*/g)].map((match) => match[1]);
      requirementCount += requirementIds.length;
      for (const requirementId of requirementIds) {
        if (!qualifiedRequirements.has(`${slice.id}:${requirementId}`)) {
          errors.push(`${path.relative(root, specFile)} ${requirementId} has no reverse entry in specs/traceability.md`);
        }
      }
      const declaredDecisions = new Set((spec.match(/Owner decisions?:\s*([^\r\n]+)/)?.[1] || '').match(/D-\d{3}/g) || []);
      const sliceRows = new Set(slice.workbookRows);
      const expectedDecisions = new Set([...decisionRows.entries()]
        .filter(([, rows]) => [...rows].some((row) => sliceRows.has(row)))
        .map(([decision]) => decision));
      const missing = [...expectedDecisions].filter((decision) => !declaredDecisions.has(decision));
      const extra = [...declaredDecisions].filter((decision) => !expectedDecisions.has(decision));
      if (missing.length) errors.push(`${path.relative(root, specFile)} omits owner decisions ${missing.join(',')}`);
      if (extra.length) errors.push(`${path.relative(root, specFile)} over-declares owner decisions ${extra.join(',')}`);
    }

    const tasksFile = path.join(directory, 'tasks.md');
    if (!implementationStarted && fs.existsSync(tasksFile) && /^\s*- \[x\]/im.test(fs.readFileSync(tasksFile, 'utf8'))) {
      errors.push(`${path.relative(root, tasksFile)} contains completed tasks before implementation`);
    }

    for (const rowNumber of slice.workbookRows) {
      if (ownership.has(rowNumber)) {
        errors.push(`Workbook row ${rowNumber} belongs to both ${ownership.get(rowNumber)} and ${slice.id}`);
      }
      ownership.set(rowNumber, slice.id);

      const row = sheet.getRow(rowNumber);
      const expectedEvidence = `specs/${slice.id}-${slice.slug}/`;
      if (cellText(row.getCell(12)).trim() !== 'Yes') {
        errors.push(`Workbook row ${rowNumber} is not Covered in SDD = Yes`);
      }
      if (cellText(row.getCell(13)).trim() !== 'No') {
        errors.push(`Workbook row ${rowNumber} is not Deferred in SDD = No`);
      }
      if (!cellText(row.getCell(14)).includes(expectedEvidence)) {
        errors.push(`Workbook row ${rowNumber} does not cite ${expectedEvidence}`);
      }
      if (!implementationStarted && cellText(row.getCell(9)).trim() !== '') {
        errors.push(`Workbook row ${rowNumber} claims target implementation before Stage 7`);
      }
    }
  }

  for (const rowNumber of expectedRows) {
    if (!ownership.has(rowNumber)) errors.push(`Workbook detail row ${rowNumber} has no SDD slice`);
  }
  for (const rowNumber of ownership.keys()) {
    if (!expectedRows.includes(rowNumber)) errors.push(`Coverage references non-detail row ${rowNumber}`);
  }

  if (errors.length) {
    errors.forEach((error) => console.error(`FAIL ${error}`));
    console.error(`SDD AUDIT FAILED: ${errors.length} violation(s)`);
    process.exit(1);
  }

  console.log(`SDD AUDIT OK: ${expectedRows.length} rows, ${coverage.slices.length} slices, ${artifactCount} artifacts, ${requirementCount} feature-qualified requirements`);
}

if (require.main === module) {
  run().catch((error) => {
    console.error(error.stack || error.message);
    process.exit(1);
  });
}

module.exports = { implementationHasStarted };
