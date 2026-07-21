'use strict';

const fs = require('fs');
const path = require('path');
const ExcelJS = require('exceljs');
const { cellText } = require('./lib');

const FILE = path.join(__dirname, '..', 'legacy_user_flows.xlsx');
const rootDir = path.join(__dirname, '..', '..');

(async () => {
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(FILE);
  const main = wb.getWorksheet('User Flows');
  if (!main) throw new Error('Sheet "User Flows" not found');

  const errors = [];
  let checkedCount = 0;

  function checkFile(filePath, lineSpec, rowNumber) {
    // If filepath contains a wildcard *, check if any files match
    if (filePath.includes('*')) {
      const baseDir = path.join(rootDir, path.dirname(filePath));
      const pattern = path.basename(filePath);
      if (!fs.existsSync(baseDir)) {
        errors.push(`Row ${rowNumber}: Base directory not found: ${path.dirname(filePath)}`);
        return;
      }
      const files = fs.readdirSync(baseDir);
      const regex = new RegExp('^' + pattern.replace(/\./g, '\\.').replace(/\*/g, '.*') + '$');
      const matched = files.filter(f => regex.test(f));
      if (matched.length === 0) {
        errors.push(`Row ${rowNumber}: Wildcard path matched no files: ${filePath}`);
      }
      return;
    }

    const fullPath = path.join(rootDir, filePath);
    if (!fs.existsSync(fullPath)) {
      errors.push(`Row ${rowNumber}: File not found: ${filePath}`);
      return;
    }
    const stats = fs.statSync(fullPath);
    if (stats.isDirectory()) {
      return;
    }
    if (lineSpec) {
      const content = fs.readFileSync(fullPath, 'utf8');
      const lines = content.split(/\r?\n/);
      const lineCount = lines.length;

      // clean line spec (e.g. "52-66")
      const cleanSpec = lineSpec.replace(/[^\d\-]/g, '');
      const m = cleanSpec.match(/^(\d+)(?:-(\d+))?$/);
      if (m) {
        const start = parseInt(m[1], 10);
        const end = m[2] ? parseInt(m[2], 10) : start;
        if (start > lineCount) {
          errors.push(`Row ${rowNumber}: Line spec ${lineSpec} starts at ${start} but file ${filePath} has only ${lineCount} lines`);
        } else if (end > lineCount) {
          errors.push(`Row ${rowNumber}: Line spec ${lineSpec} ends at ${end} but file ${filePath} has only ${lineCount} lines`);
        }
      }
    }
  }

  main.eachRow({ includeEmpty: true }, (row, r) => {
    if (r < 7) return; // Skip headers
    const useCaseId = cellText(row.getCell(1)).trim();
    if (useCaseId !== "") {
      // Epic banner row
      return;
    }
    
    const evidence = cellText(row.getCell(8)).trim();
    if (!evidence) {
      errors.push(`Row ${r}: No legacy source evidence cited`);
      return;
    }

    checkedCount++;
    const citations = evidence.split(';');
    for (const cit of citations) {
      const trimmed = cit.trim();
      if (trimmed.startsWith('Runtime:') || trimmed.startsWith('harness:') || trimmed.startsWith('basis:')) {
        continue;
      }
      // Match path starting with legacy/ and optional line spec
      const match = trimmed.match(/^(legacy\/[\w\.\-\/\*]+)(?::(\d+(?:\-\d+)?))?/);
      if (match) {
        checkFile(match[1], match[2], r);
      } else if (trimmed.startsWith('legacy/')) {
        const firstWord = trimmed.split(/\s+/)[0];
        const matchFallback = firstWord.match(/^(legacy\/[\w\.\-\/\*]+)(?::(\d+(?:\-\d+)?))?/);
        if (matchFallback) {
          checkFile(matchFallback[1], matchFallback[2], r);
        } else {
          errors.push(`Row ${r}: Could not parse citation starting with legacy: "${trimmed}"`);
        }
      }
    }
  });

  console.log(`Checked evidence in ${checkedCount} rows.`);
  if (errors.length > 0) {
    console.error(`Found ${errors.length} errors:`);
    errors.forEach(e => console.error('  ' + e));
    process.exit(1);
  } else {
    console.log('All legacy source evidence files and line references exist and are valid!');
    process.exit(0);
  }
})().catch(e => {
  console.error(e);
  process.exit(1);
});
