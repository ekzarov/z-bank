// artifact-tool currently exports workbook.xml under a generic XML content
// type. Excel and ExcelJS require the standard workbook override. This script
// repairs packaging metadata and restores row outlines that artifact-tool does
// not currently expose. Workbook cell values and status styles are untouched.
'use strict';

const fs = require('fs');
const path = require('path');
const JSZip = require('jszip');
const ExcelJS = require('exceljs');

async function repair(file) {
  const zip = await JSZip.loadAsync(fs.readFileSync(file));
  const entry = zip.file('[Content_Types].xml');
  if (!entry) throw new Error(`${file}: [Content_Types].xml not found`);
  let xml = await entry.async('string');
  xml = xml.replace(
    '<Default Extension="xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />',
    '<Default Extension="xml" ContentType="application/xml" /><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />'
  );
  zip.file('[Content_Types].xml', xml);

  // ExcelJS' SAX parsers expect the default spreadsheet namespace and do not
  // recognize artifact-tool's equivalent x:-prefixed element names.
  const spreadsheetXml = [
    'xl/workbook.xml',
    'xl/styles.xml',
    'xl/sharedStrings.xml',
    'xl/worksheets/sheet1.xml',
  ];
  for (const name of spreadsheetXml) {
    const part = zip.file(name);
    if (!part) continue;
    let partXml = await part.async('string');
    partXml = partXml.replace(/<(\/?)x:/g, '<$1');
    partXml = partXml.replace('xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"', 'xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"');
    zip.file(name, partXml);
  }
  const output = await zip.generateAsync({ type: 'nodebuffer', compression: 'DEFLATE' });
  fs.writeFileSync(file, output);

  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(file);
  const main = workbook.getWorksheet('User Flows');
  if (!main) throw new Error(`${file}: User Flows sheet not found`);
  main.properties.outlineLevelRow = 1;
  main.properties.outlineProperties = { summaryBelow: false, summaryRight: true };
  for (let rowNumber = 7; rowNumber <= main.rowCount; rowNumber++) {
    const row = main.getRow(rowNumber);
    const id = String(row.getCell(1).value || '');
    row.outlineLevel = /^UF-\d+$/.test(id) ? 0 : 1;
    row.hidden = false;
  }
  await workbook.xlsx.writeFile(file);
  console.log(`repaired and outlined ${file}`);
}

(async () => {
  const files = process.argv.slice(2);
  if (!files.length) throw new Error('Usage: node repair-artifact-xlsx.js <file> [file...]');
  for (const file of files) await repair(path.resolve(file));
})().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
