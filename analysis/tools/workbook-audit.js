// Invariant checker for analysis/legacy_user_flows.xlsx. Run before committing any
// workbook change: `node analysis/tools/workbook-audit.js` (exit 0 = all green).
//
// Invariants (see legacy_user_flows_template_instructions.md):
//  A. Colour = status on every scenario row:
//     green  ⇔ Destination implemented? (I) = Yes
//     orange ⇒ I ≠ Yes AND Deferred in SDD? (M) = Yes AND a reason in Destination notes (J)
//     red    ⇒ I ≠ Yes AND M ≠ Yes (the actionable list)
//  B. Epic banner rows: all children Yes → green + Scenario column (C) = Passed;
//     any red child → red; otherwise orange.
//  C. Revision coverage: every open row (I ≠ Yes) is referenced by at least one
//     "Rev N" sheet (column «Строка листа 1»).
//  D. Revision sheets: a green (closed) finding must carry Implemented? = Yes.
//  E. Rev-sheet rows use only the canonical types: gap / decision / deferred.
//  H. Every detail row carries a controlled Runtime label, printable rows use
//     automatic height, and continuation pages repeat their headers.
'use strict';

const path = require('path');
const ExcelJS = require('exceljs');
const { COLORS, cellText, fillArgb, parseRefs } = require('./lib');

const FILE = path.join(__dirname, '..', 'legacy_user_flows.xlsx');
const DATA_START = 7;

(async () => {
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(FILE);
  const main = wb.getWorksheet('User Flows');
  if (!main) throw new Error('Sheet "User Flows" not found');

  const errors = [];
  const epics = []; // { headerRow, children: [rows] }
  main.eachRow((row, r) => {
    if (r < DATA_START) return;
    if (/^UF-\d+/.test(cellText(row.getCell(1)))) epics.push({ headerRow: r, children: [] });
    else if (epics.length) epics[epics.length - 1].children.push(r);
  });

  // A: colour = status on scenario rows, uniformly across D:N.
  const openRows = [];
  for (const e of epics) {
    for (const r of e.children) {
      const row = main.getRow(r);
      const done = cellText(row.getCell(9)).trim() === 'Yes';
      const deferred = cellText(row.getCell(13)).trim() === 'Yes';
      const reason = cellText(row.getCell(10)).trim() !== '';
      const fill = fillArgb(row.getCell(9));
      const expectedFill = done ? COLORS.GREEN : deferred ? COLORS.ORANGE : COLORS.RED;
      if (!done) openRows.push(r);
      if (done && fill !== COLORS.GREEN) errors.push(`A r${r}: I=Yes but fill is ${fill} (expected green)`);
      if (!done && fill === COLORS.GREEN) errors.push(`A r${r}: green fill with I≠Yes (forbidden)`);
      if (fill === COLORS.ORANGE && !done && !deferred) errors.push(`A r${r}: orange without Deferred-in-SDD M=Yes`);
      if (fill === COLORS.ORANGE && !done && !reason) errors.push(`A r${r}: orange without a reason in Destination notes (J)`);
      if (fill === COLORS.RED && (done || deferred)) errors.push(`A r${r}: red but I=Yes or M=Yes`);
      if (![COLORS.GREEN, COLORS.ORANGE, COLORS.RED].includes(fill)) errors.push(`A r${r}: unexpected I fill ${fill}`);
      for (let c = 4; c <= 14; c++) {
        const cellFill = fillArgb(row.getCell(c));
        if (cellFill !== expectedFill) errors.push(`A r${r} c${c}: fill ${cellFill}, expected ${expectedFill}`);
      }
    }
  }

  // B: epic banner rows use one status fill and canonical typography across A:N.
  for (const e of epics) {
    const hdr = main.getRow(e.headerRow);
    const uf = cellText(hdr.getCell(1)).slice(0, 6);
    const allYes = e.children.every((r) => cellText(main.getRow(r).getCell(9)).trim() === 'Yes');
    const anyRed = e.children.some((r) => fillArgb(main.getRow(r).getCell(9)) === COLORS.RED);
    const fill = fillArgb(hdr.getCell(1));
    const expected = allYes ? COLORS.GREEN : anyRed ? COLORS.RED : COLORS.ORANGE;
    if (fill !== expected) errors.push(`B ${uf} r${e.headerRow}: header fill ${fill}, expected ${expected}`);
    const expectedFontColor = expected === COLORS.RED ? 'FF7F0000' : 'FF1F2937';
    for (let c = 1; c <= 14; c++) {
      const cell = hdr.getCell(c);
      const font = cell.font || {};
      const fontColor = font.color && font.color.argb ? font.color.argb.toUpperCase() : null;
      const cellFill = fillArgb(cell);
      if (cellFill !== expected) errors.push(`B ${uf} r${e.headerRow} c${c}: fill ${cellFill}, expected ${expected}`);
      if (font.name !== 'Carlito' || font.size !== 11 || font.bold !== true || fontColor !== expectedFontColor) {
        errors.push(`B ${uf} r${e.headerRow} c${c}: font ${font.name}/${font.size}/bold=${font.bold}/color=${fontColor}, expected Carlito/11/bold=true/color=${expectedFontColor}`);
      }
    }
    if (allYes && cellText(hdr.getCell(3)).trim() !== 'Passed') errors.push(`B ${uf}: fully green but Scenario column ≠ Passed`);
  }

  // C: revision coverage of open rows. A code-only discovery map legitimately
  // has no revision worklist yet; enforcement starts as soon as destination/SDD
  // lifecycle data or a Rev sheet exists.
  const covered = new Set();
  const detailRows = new Set(epics.flatMap((epic) => epic.children));
  const revSheets = wb.worksheets.filter((ws) => /^Rev \d+$/.test(ws.name));
  const lifecycleStarted = epics.some((e) => e.children.some((r) => {
    const row = main.getRow(r);
    for (let c = 9; c <= 14; c++) if (cellText(row.getCell(c)).trim() !== '') return true;
    return false;
  }));
  for (const ws of revSheets) {
    ws.eachRow((row, r) => {
      if (r === 1) return;
      parseRefs(cellText(row.getCell(3))).forEach((x) => {
        if (!detailRows.has(x)) errors.push(`C ${ws.name} r${r}: reference ${x} is not a detail row`);
        covered.add(x);
      });
    });
  }
  if (lifecycleStarted || revSheets.length > 0) {
    for (const r of openRows) {
      if (!covered.has(r)) errors.push(`C r${r}: open row not referenced by any Rev sheet`);
    }
  }

  // D: closed findings on Rev sheets carry Implemented? = Yes
  for (const ws of wb.worksheets) {
    if (!/^Rev \d+$/.test(ws.name)) continue;
    ws.eachRow((row, r) => {
      if (r === 1) return;
      const green = fillArgb(row.getCell(1)) === COLORS.GREEN;
      const impl = cellText(row.getCell(8)).trim() === 'Yes';
      if (green && !impl) errors.push(`D ${ws.name} r${r}: green finding without Implemented?=Yes`);
    });
  }

  // G: canonical typography and the light A:C detail spine are stable.
  for (const e of epics) {
    for (const r of e.children) {
      const row = main.getRow(r);
      for (let c = 1; c <= 14; c++) {
        const font = row.getCell(c).font || {};
        if (font.name !== 'Carlito' || font.size !== 10) {
          errors.push(`G User Flows r${r} c${c}: font ${font.name}/${font.size}, expected Carlito/10`);
        }
      }
      for (let c = 1; c <= 3; c++) {
        const fill = fillArgb(row.getCell(c));
        if (fill !== 'FFF8FAFC') errors.push(`G User Flows r${r} c${c}: spine fill ${fill}, expected FFF8FAFC`);
      }
    }
  }
  for (const ws of revSheets) {
    ws.eachRow((row, r) => {
      for (let c = 1; c <= 9; c++) {
        const font = row.getCell(c).font || {};
        const expectedSize = r === 1 ? 11 : 10;
        if (font.name !== 'Carlito' || font.size !== expectedSize) {
          errors.push(`G ${ws.name} r${r} c${c}: font ${font.name}/${font.size}, expected Carlito/${expectedSize}`);
        }
      }
    });
  }

  // H: runtime provenance and printable layout remain explicit and readable.
  const runtimePattern = /Runtime:\s*(live-observed|simulated|static-only|waived)\b/i;
  for (const e of epics) {
    for (const r of e.children) {
      const row = main.getRow(r);
      if (!runtimePattern.test(cellText(row.getCell(8)))) errors.push(`H User Flows r${r}: missing controlled Runtime label`);
      if (row.height !== undefined) errors.push(`H User Flows r${r}: fixed row height ${row.height} can clip wrapped text`);
    }
  }
  if (main.pageSetup.printTitlesRow !== '1:6') errors.push(`H User Flows: printTitlesRow=${main.pageSetup.printTitlesRow}, expected 1:6`);
  for (const ws of revSheets) {
    if (ws.pageSetup.printTitlesRow !== '1:1') errors.push(`H ${ws.name}: printTitlesRow=${ws.pageSetup.printTitlesRow}, expected 1:1`);
    ws.eachRow((row) => {
      if (row.height !== undefined) errors.push(`H ${ws.name} r${row.number}: fixed row height ${row.height} can clip wrapped text`);
    });
  }

  // E: only canonical finding types on Rev sheets (unverified suspicions ⇒ red gap)
  for (const ws of wb.worksheets) {
    if (!/^Rev \d+$/.test(ws.name)) continue;
    ws.eachRow((row, r) => {
      if (r === 1) return;
      const type = cellText(row.getCell(4)).trim().toLowerCase();
      const rowFill = fillArgb(row.getCell(1));
      for (let c = 2; c <= 9; c++) {
        const cellFill = fillArgb(row.getCell(c));
        if (cellFill !== rowFill) errors.push(`F ${ws.name} r${r} c${c}: fill ${cellFill}, expected uniform ${rowFill}`);
      }
      if (!['gap', 'decision', 'deferred'].includes(type)) errors.push(`E ${ws.name} r${r}: non-canonical type «${type}» — use gap / decision / deferred (unverified ⇒ commit as red gap)`);
    });
  }

  const openCount = openRows.length;
  const total = epics.reduce((s, e) => s + e.children.length, 0);
  console.log(`scenario rows: ${total} (closed ${total - openCount}, open ${openCount}); epics: ${epics.length}; rev-covered open rows: ${openRows.filter((r) => covered.has(r)).length}/${openCount}`);
  if (errors.length) {
    for (const e of errors) console.error('FAIL ' + e);
    console.error(`AUDIT FAILED: ${errors.length} violation(s)`);
    process.exit(1);
  }
  console.log('AUDIT OK');
})().catch((e) => { console.error(e.message); process.exit(1); });
