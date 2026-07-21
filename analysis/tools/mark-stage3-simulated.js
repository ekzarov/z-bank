'use strict';

const path = require('path');
const ExcelJS = require('exceljs');
const { cellText, snapshot, diffAgainst } = require('./lib');
const fixture = require('../../simulation/fixtures/legacy-fixture.json');

async function main() {
  const file = path.resolve(__dirname, '..', 'legacy_user_flows.xlsx');
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(file);
  const before = new Map(workbook.worksheets.map((sheet) => [sheet.name, snapshot(sheet)]));
  const sheet = workbook.getWorksheet('User Flows');
  if (!sheet) throw new Error('User Flows sheet not found');

  const expected = new Set();
  for (const [key, evidence] of Object.entries(fixture.evidence)) {
    const label = `Runtime: simulated — simulation/fixtures/legacy-fixture.json#evidence.${key}; ` +
      `harness: simulation/test; basis: ${evidence.sources.join('; ')}`;
    for (const rowNumber of evidence.workbookRows) {
      const cell = sheet.getRow(rowNumber).getCell(8);
      const current = cellText(cell);
      if (!current.includes(label)) {
        cell.value = `${current}; ${label}`;
      }
      expected.add(`${rowNumber}:8`);
    }
  }

  for (const worksheet of workbook.worksheets) {
    const changes = diffAgainst(worksheet, before.get(worksheet.name));
    const allowed = worksheet.name === 'User Flows' ? expected : new Set();
    const unexpected = changes.filter((key) => !allowed.has(key));
    if (unexpected.length) {
      throw new Error(`Unexpected workbook mutations in ${worksheet.name}: ${unexpected.join(', ')}`);
    }
  }

  await workbook.xlsx.writeFile(file);
  console.log(`Marked ${expected.size} rows as Runtime: simulated`);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
