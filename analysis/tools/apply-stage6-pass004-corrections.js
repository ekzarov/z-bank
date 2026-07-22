'use strict';

const path = require('path');
const ExcelJS = require('exceljs');
const { setFill } = require('./lib');

const FILE = path.join(__dirname, '..', 'legacy_user_flows.xlsx');
const LIGHT_SPINE = 'FFF8FAFC';
const EPIC_ROWS = new Set([7, 21, 28, 40, 49, 59, 79, 86, 99, 108, 118, 127]);

function setFont(cell, name, size, bold) {
  const style = JSON.parse(JSON.stringify(cell.style || {}));
  style.font = { ...(style.font || {}), name, size };
  if (bold !== undefined) style.font.bold = bold;
  cell.style = style;
}

(async () => {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(FILE);
  const userFlows = workbook.getWorksheet('User Flows');
  const rev1 = workbook.getWorksheet('Rev 1');
  if (!userFlows || !rev1) throw new Error('Expected User Flows and Rev 1 sheets.');

  const updates = {
    J12: 'Deviation D-001: one accessible target navigation model replaces channel-specific control panels and terminal menus.',
    D14: 'Validate the legacy channel-prefixed customer identifier before calling the API.',
    E14: 'The legacy web channel requires a C/IMS-prefixed identifier and reports malformed input before sending a request.',
    F14: 'No backend request is sent until the legacy channel-prefixed identifier is valid.',
    H14: 'legacy/src/frontend/js/utils.js:99-141; customer-details.html:159-169',
    J14: 'Deviation D-001: target customer IDs are channel-neutral; C/IMS prefixes and channel routing are not ported.',
    D32: 'Reject customer creation when no credit agency returns a score.',
    E32: 'Credit-agency timeout or total failure ends the creation request before customer persistence.',
    F32: 'A provider failure is reported and no partial customer is created.',
    H32: 'legacy/src/base/cics/cobol/CRECUST.cbl:363-384,958',
    D51: 'Accept only ISA, CURRENT, LOAN, SAVING, or MORTGAGE; validate customer, interest, overdraft, and the maximum of 10 existing accounts per customer.',
    E51: 'Invalid product parameters or an existing portfolio of 10 accounts are rejected before persistence.',
    F51: 'The operator receives a validation message and no account is created.',
    H51: 'legacy/src/base/cics/cobol/BNK1CAC.cbl:428-745,897-902; CREACC.cbl:380-392',
    D54: 'Update account type, interest, and overdraft while protecting balances; statement dates are accepted and validated by the screen but not persisted.',
    E54: 'The legacy update path changes only persisted product metadata; entered statement-date changes are silently discarded.',
    F54: 'Type, interest, and overdraft changes are saved; balances and stored statement dates remain unchanged.',
    G54: 'Partial',
    H54: 'legacy/src/base/cics/cobol/BNK1UAC.cbl:506-751,885-960; UPDACC.cbl:274-323',
    J54: 'Decision D-016: target statement dates are system-managed and are not editable account metadata.',
    J57: 'Decision D-017: replace unconditional hard-delete with eligibility-gated account closure that retains history.',
  };

  for (const [address, value] of Object.entries(updates)) userFlows.getCell(address).value = value;

  rev1.getCell('C11').value = '8-14,113';
  const decisionRows = [
    ['R1-D-016', null, '54', 'decision', 'The legacy screen accepts statement dates that the update program does not persist.', 'Owner-approved target decision D-016 recorded in Stage 4.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-016; approved by project owner on 2026-07-22.'],
    ['R1-D-017', null, '57', 'decision', 'Legacy account deletion is unconditional and removes the account record.', 'Owner-approved target decision D-017 recorded in Stage 4.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-017; approved by project owner on 2026-07-22.'],
  ];
  const knownDecisionIds = new Set();
  rev1.eachRow((row) => knownDecisionIds.add(String(row.getCell(1).value || '')));
  for (const values of decisionRows) {
    if (knownDecisionIds.has(values[0])) continue;
    const row = rev1.addRow(values);
    const styleSource = rev1.getRow(row.number - 1);
    for (let column = 1; column <= 9; column += 1) {
      row.getCell(column).style = JSON.parse(JSON.stringify(styleSource.getCell(column).style || {}));
    }
  }

  for (let rowNumber = 7; rowNumber <= 153; rowNumber += 1) {
    if (EPIC_ROWS.has(rowNumber)) continue;
    const row = userFlows.getRow(rowNumber);
    for (let column = 1; column <= 14; column += 1) setFont(row.getCell(column), 'Carlito', 10);
    for (let column = 1; column <= 3; column += 1) setFill(row.getCell(column), LIGHT_SPINE);
  }

  rev1.eachRow({ includeEmpty: true }, (row, rowNumber) => {
    for (let column = 1; column <= 9; column += 1) {
      setFont(row.getCell(column), 'Carlito', rowNumber === 1 ? 11 : 10, rowNumber === 1 ? true : undefined);
    }
  });

  await workbook.xlsx.writeFile(FILE);
  console.log('Applied Stage 6 Pass 004 workbook corrections.');
})().catch((error) => {
  console.error(error.stack || error.message);
  process.exit(1);
});
