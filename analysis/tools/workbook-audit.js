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

const fs = require('fs');
const path = require('path');
const ExcelJS = require('exceljs');
const { COLORS, cellText, fillArgb, parseRefs } = require('./lib');

const FILE = path.join(__dirname, '..', 'legacy_user_flows.xlsx');
const TRACEABILITY_FILE = path.join(__dirname, '..', '..', 'specs', 'traceability.md');
const COVERAGE_FILE = path.join(__dirname, '..', 'stage-05-sdd-coverage.json');
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

  const lifecycleNote = cellText(main.getRow(2).getCell(1));
  if (!lifecycleNote.includes('SDD coverage complete')
    || !lifecycleNote.includes('Destination implementation remains open')) {
    errors.push('B User Flows r2: lifecycle note must distinguish complete SDD coverage from open destination implementation');
  }
  const openLegend = cellText(main.getRow(4).getCell(5));
  if (!openLegend.includes('SDD-covered')
    || !openLegend.includes('destination implementation is not complete')) {
    errors.push('B User Flows r4: red legend must describe SDD-covered but incomplete destination work');
  }
  const deferredLegend = cellText(main.getRow(4).getCell(8));
  if (!deferredLegend.includes('explicit owner-approved deferral') || !deferredLegend.includes('reason')) {
    errors.push('B User Flows r4: orange legend must require an explicit owner-approved deferral and reason');
  }
  const neutralLegend = cellText(main.getRow(4).getCell(11));
  if (neutralLegend !== 'Neutral formatting only; it does not express lifecycle status.') {
    errors.push('B User Flows r4: white legend must be lifecycle-neutral');
  }

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
    const expectedStatus = allYes ? 'Passed' : anyRed ? 'Not Passed - Open' : 'Not Passed - Deferred';
    const expectedDescription = allYes
      ? 'Legacy behavior is evidenced, SDD-covered, implemented, and accepted.'
      : anyRed
        ? 'Legacy behavior is evidenced and covered by SDD; target implementation has not started.'
        : 'Legacy behavior is evidenced and covered by SDD; deferred work has an approved reason.';
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
    if (cellText(hdr.getCell(3)).trim() !== expectedStatus) {
      errors.push(`B ${uf}: Scenario column is not ${expectedStatus}`);
    }
    if (cellText(hdr.getCell(4)).trim() !== expectedDescription) {
      errors.push(`B ${uf}: lifecycle description is not canonical for ${expectedStatus}`);
    }
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

  // C2: decision references are bidirectionally identical between destination
  // notes and Rev sheets. This prevents a decision from silently claiming too
  // many rows or omitting a row that depends on it.
  const noteDecisionRows = new Map();
  for (const rowNumber of detailRows) {
    const notes = cellText(main.getRow(rowNumber).getCell(10));
    for (const match of notes.matchAll(/D-\d{3}/g)) {
      if (!noteDecisionRows.has(match[0])) noteDecisionRows.set(match[0], new Set());
      noteDecisionRows.get(match[0]).add(rowNumber);
    }
  }
  const revDecisionRows = new Map();
  for (const ws of revSheets) {
    ws.eachRow((row, r) => {
      if (r === 1) return;
      const decisionId = cellText(row.getCell(1)).match(/D-\d{3}/)?.[0];
      if (!decisionId) return;
      if (revDecisionRows.has(decisionId)) errors.push(`C2 ${decisionId}: appears more than once on Rev sheets`);
      revDecisionRows.set(decisionId, new Set(parseRefs(cellText(row.getCell(3)))));
    });
  }
  for (const decisionId of new Set([...noteDecisionRows.keys(), ...revDecisionRows.keys()])) {
    const notes = noteDecisionRows.get(decisionId) || new Set();
    const revision = revDecisionRows.get(decisionId) || new Set();
    const missingFromRevision = [...notes].filter((row) => !revision.has(row));
    const missingFromNotes = [...revision].filter((row) => !notes.has(row));
    if (missingFromRevision.length) errors.push(`C2 ${decisionId}: Rev sheet omits note rows ${missingFromRevision.join(',')}`);
    if (missingFromNotes.length) errors.push(`C2 ${decisionId}: Rev sheet over-attributes rows ${missingFromNotes.join(',')}`);
  }
  const traceDecisionRows = new Map();
  const traceability = fs.readFileSync(TRACEABILITY_FILE, 'utf8');
  for (const match of traceability.matchAll(/^\|\s*([^|]+)\|\s*([^|]+)\|/gm)) {
    const rowRefs = parseRefs(match[1]);
    if (!rowRefs.length) continue;
    for (const decision of match[2].matchAll(/D-\d{3}/g)) {
      if (!traceDecisionRows.has(decision[0])) traceDecisionRows.set(decision[0], new Set());
      rowRefs.forEach((row) => traceDecisionRows.get(decision[0]).add(row));
    }
  }
  for (const decisionId of new Set([...noteDecisionRows.keys(), ...traceDecisionRows.keys()])) {
    const notes = noteDecisionRows.get(decisionId) || new Set();
    const trace = traceDecisionRows.get(decisionId) || new Set();
    const missingFromTrace = [...notes].filter((row) => !trace.has(row));
    const missingFromNotes = [...trace].filter((row) => !notes.has(row));
    if (missingFromTrace.length) errors.push(`C2 ${decisionId}: traceability omits note rows ${missingFromTrace.join(',')}`);
    if (missingFromNotes.length) errors.push(`C2 ${decisionId}: traceability over-attributes rows ${missingFromNotes.join(',')}`);
  }
  const coverageDecisionRows = new Map();
  const coverage = JSON.parse(fs.readFileSync(COVERAGE_FILE, 'utf8'));
  for (const [references, note] of Object.entries(coverage.decisionNotes || {})) {
    for (const decision of note.matchAll(/D-\d{3}/g)) {
      if (!coverageDecisionRows.has(decision[0])) coverageDecisionRows.set(decision[0], new Set());
      parseRefs(references).forEach((row) => coverageDecisionRows.get(decision[0]).add(row));
    }
  }
  for (const decisionId of new Set([...noteDecisionRows.keys(), ...coverageDecisionRows.keys()])) {
    const notes = noteDecisionRows.get(decisionId) || new Set();
    const coverageRows = coverageDecisionRows.get(decisionId) || new Set();
    const missingFromCoverage = [...notes].filter((row) => !coverageRows.has(row));
    const missingFromNotes = [...coverageRows].filter((row) => !notes.has(row));
    if (missingFromCoverage.length) errors.push(`C2 ${decisionId}: coverage JSON omits note rows ${missingFromCoverage.join(',')}`);
    if (missingFromNotes.length) errors.push(`C2 ${decisionId}: coverage JSON over-attributes rows ${missingFromNotes.join(',')}`);
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
        const cell = row.getCell(c);
        const font = cell.font || {};
        const fontColor = font.color && font.color.argb ? font.color.argb.toUpperCase() : null;
        const expectedBold = c === 2 && cellText(cell).trim() !== '';
        if (font.name !== 'Carlito' || font.size !== 10 || fontColor !== 'FF000000'
          || Boolean(font.bold) !== expectedBold) {
          errors.push(`G User Flows r${r} c${c}: font ${font.name}/${font.size}/${fontColor}/bold=${font.bold}, expected Carlito/10/FF000000/bold=${expectedBold}`);
        }
        for (const side of ['left', 'right', 'top', 'bottom']) {
          const border = (cell.border || {})[side] || {};
          const borderColor = border.color && border.color.argb ? border.color.argb.toUpperCase() : null;
          if (border.style !== 'thin' || borderColor !== 'FFE7C8BD') {
            errors.push(`G User Flows r${r} c${c}: ${side} border ${border.style}/${borderColor}, expected thin/FFE7C8BD`);
          }
        }
        const alignment = cell.alignment || {};
        if (alignment.wrapText !== true || alignment.vertical !== 'top'
          || ![undefined, 'left'].includes(alignment.horizontal)) {
          errors.push(`G User Flows r${r} c${c}: invalid alignment ${JSON.stringify(alignment)}`);
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
  if (main.getRow(4).height !== 54) errors.push(`H User Flows r4: height=${main.getRow(4).height}, expected 54`);
  for (const epic of epics) {
    if (main.getRow(epic.headerRow).height !== 40) errors.push(`H User Flows r${epic.headerRow}: epic height=${main.getRow(epic.headerRow).height}, expected 40`);
  }
  for (const ws of revSheets) {
    if (ws.pageSetup.printTitlesRow !== '1:1') errors.push(`H ${ws.name}: printTitlesRow=${ws.pageSetup.printTitlesRow}, expected 1:1`);
    ws.eachRow((row) => {
      if (row.number === 1 && row.height !== 40) errors.push(`H ${ws.name} r1: height=${row.height}, expected 40`);
      if (row.number !== 1 && row.height !== undefined) errors.push(`H ${ws.name} r${row.number}: fixed row height ${row.height} can clip wrapped text`);
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
  console.log(`scenario rows: ${total} (closed ${total - openCount}, open ${openCount}); epics: ${epics.length}; decisions: ${revDecisionRows.size}; rev-covered open rows: ${openRows.filter((r) => covered.has(r)).length}/${openCount}`);
  if (errors.length) {
    for (const e of errors) console.error('FAIL ' + e);
    console.error(`AUDIT FAILED: ${errors.length} violation(s)`);
    process.exit(1);
  }
  console.log('AUDIT OK');
})().catch((e) => { console.error(e.message); process.exit(1); });
