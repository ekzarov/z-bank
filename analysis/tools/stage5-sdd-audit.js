'use strict';

const fs = require('fs');
const path = require('path');
const ExcelJS = require('exceljs');
const { cellText } = require('./lib');

const root = path.resolve(__dirname, '..', '..');
const coverageFile = path.join(root, 'analysis', 'stage-05-sdd-coverage.json');
const workbookFile = path.join(root, 'analysis', 'legacy_user_flows.xlsx');

(async () => {
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
  for (const slice of coverage.slices) {
    const directory = path.join(root, 'specs', `${slice.id}-${slice.slug}`);
    for (const artifact of ['spec.md', 'plan.md', 'tasks.md']) {
      const file = path.join(directory, artifact);
      if (!fs.existsSync(file)) errors.push(`Missing ${path.relative(root, file)}`);
    }

    const tasksFile = path.join(directory, 'tasks.md');
    if (fs.existsSync(tasksFile) && /^\s*- \[x\]/im.test(fs.readFileSync(tasksFile, 'utf8'))) {
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
      if (cellText(row.getCell(9)).trim() !== '') {
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

  console.log(`SDD AUDIT OK: ${expectedRows.length} rows, ${coverage.slices.length} slices, 27 artifacts`);
})().catch((error) => {
  console.error(error.stack || error.message);
  process.exit(1);
});
