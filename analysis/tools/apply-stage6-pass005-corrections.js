'use strict';

const path = require('path');
const ExcelJS = require('exceljs');

const file = path.join(__dirname, '..', 'legacy_user_flows.xlsx');
const epicRows = new Set([7, 21, 28, 40, 49, 59, 79, 86, 99, 108, 118, 127]);
const staticOnlyRows = new Set([24, 25, 41, 42, 43, 44, 45, 46, 47, 48,
  50, 51, 52, 53, 54, 55, 81, 82, 88, 93, 95, 98, 100, 101, 102, 103,
  105, 106, 107, 115, 116]);

function set(row, column, value) {
  row.getCell(column).value = value;
}

function appendRuntimeLabel(value, label) {
  const text = String(value || '').replace(/; Runtime: [^;]+(?:; harness: [^;]+; basis: .*)?$/u, '');
  return `${text}; Runtime: ${label}`;
}

(async () => {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(file);
  const flows = workbook.getWorksheet('User Flows');
  const rev = workbook.getWorksheet('Rev 1');
  if (!flows || !rev) throw new Error('Expected User Flows and Rev 1 sheets.');

  const d012 = 'Decision D-012: preserve statement content and period behavior through an explicit modern job/API without JCL or fixed-width pagination.';
  for (let n = 100; n <= 107; n += 1) set(flows.getRow(n), 10, d012);

  set(flows.getRow(22), 4, 'Retrieve and display customer identity, address, credit score, and review date.');
  set(flows.getRow(22), 6, 'The matching identity, address, date-of-birth, credit score, and review date are displayed; the map has no customer-status field.');

  set(flows.getRow(26), 10, 'Decision D-018: implement normalized name search instead of the unsupported placeholder.');
  set(flows.getRow(27), 4, 'Report an unknown customer, noting that the legacy page can retain previously selected customer state.');
  set(flows.getRow(27), 5, 'A failed lookup shows an error but does not explicitly clear currentCustomer or all stale details.');
  set(flows.getRow(27), 6, 'An error notification is shown; stale selection/detail state can remain.');
  set(flows.getRow(27), 7, 'Partial');
  set(flows.getRow(27), 10, 'Decision D-018: target failed lookups clear stale state, finish loading, and disable mutations.');

  set(flows.getRow(33), 4, 'Attempt customer creation after advancing the CONTROL counter; runtime rollback semantics are not explicit in the supplied source.');
  set(flows.getRow(33), 5, 'The counter update precedes customer insert and an insert failure returns without an explicit rollback statement.');
  set(flows.getRow(33), 6, 'Customer creation failure is reported, but gap-free counter rollback is not code-proven without CICS/DB2 runtime evidence.');
  set(flows.getRow(33), 7, 'Partial');
  set(flows.getRow(33), 8, 'legacy/src/base/cics/cobol/CRECUST.cbl:1219-1268,1464-1470,1496-1524; Runtime: static-only');
  set(flows.getRow(33), 10, 'Decision D-023: target allocation and persistence are atomic; identifier gaps are allowed, partial entities are not.');

  set(flows.getRow(38), 4, 'Attempt to delete up to the retrieved customer accounts and then delete the customer.');
  set(flows.getRow(38), 5, 'DELCUS retrieves at most 20 accounts and does not stop customer deletion when an individual DELACC reports failure.');
  set(flows.getRow(38), 6, 'Processed-transaction rows are attempted, but complete cascade/no-orphan behavior is not code-proven.');
  set(flows.getRow(38), 7, 'Partial');
  set(flows.getRow(38), 8, 'legacy/src/base/cics/cobol/DELCUS.cbl:343-410; Runtime: static-only');
  set(flows.getRow(38), 10, 'Decision D-009: target customer retirement is non-destructive and checks account relationships explicitly.');

  set(flows.getRow(43), 4, 'Retrieve at most 20 related accounts and render at most the first 10 on the CICS screen.');
  set(flows.getRow(43), 6, 'The CICS portfolio view displays up to 10 of the accounts returned by a 20-entry inquiry buffer.');
  set(flows.getRow(43), 8, 'legacy/src/base/cics/cobol/BNK1CCA.cbl:576-650; INQACCCU.cbl:457-520; legacy/src/base/cics/bms/BNK1ACC.bms:47-60; Runtime: static-only');
  set(flows.getRow(43), 10, 'Decision D-019: target portfolio pagination returns the complete authorized set without channel display caps.');
  set(flows.getRow(45), 10, 'Decision D-019: target portfolio pagination replaces the IMS six-entry response limit.');
  set(flows.getRow(50), 10, 'Decisions D-020/D-023: target generates account identity, sort code, dates, and zero balances atomically.');
  set(flows.getRow(51), 10, 'Decision D-019: target preserves the ten-account business limit while removing presentation caps.');
  set(flows.getRow(52), 10, 'Decision D-023: target account allocation, entity persistence, and audit are atomic.');
  set(flows.getRow(88), 10, 'Decision D-008: target Problem Details replace the three missing generated 400 response files.');
  set(flows.getRow(112), 7, 'Partial');
  set(flows.getRow(112), 6, 'The list mapping emits CHECKING; the single-account mapping references $item without a foreach and is not proven valid.');

  set(flows.getRow(80), 5, 'The operator supplies source/destination account numbers and amount; the program supplies the configured bank sort code internally.');
  set(flows.getRow(80), 10, 'Decision D-020: target derives sort codes from account/bank configuration.');
  set(flows.getRow(71), 10, 'Decision D-020: target derives the sort code from account/bank configuration instead of trusting request input.');
  set(flows.getRow(89), 10, 'Decision D-020: target derives the sort code from account/bank configuration instead of trusting request input.');
  set(flows.getRow(81), 4, 'Validate transfer input, then rely on the CICS unit of work while accounts are selected and updated in account-number order.');
  set(flows.getRow(81), 5, 'One account can be updated before a missing second account is discovered; rollback supplies atomic recovery.');
  set(flows.getRow(81), 6, 'Input errors are reported; transactional lookup/update failure rolls back the unit of work.');

  const row96 = flows.getRow(96);
  set(row96, 8, 'legacy/src/api/src/main/operations/%2Faccounts%2F%7BaccountId%7D/get/response_mapping.yaml:7; legacy/src/api/src/main/operations/%2Faccounts%2F%7BaccountId%7D%2Fbalances/get/response_mapping.yaml:7; legacy/src/api/src/main/operations/%2Fcustomers%2F%7BcustomerId%7D%2Faccounts/get/response_mapping.yaml:7; legacy/src/api/src/main/operations/%2Fcustomers%2F%7BcustomerId%7D/put/response_mapping.yaml:8-10; legacy/src/api/src/main/operations/%2Faccounts/get/response_mapping.yaml:3; legacy/src/api/src/main/operations/%2Faccounts%2F%7BaccountId%7D%2Ftransactions/get/response_mapping.yaml:3; legacy/src/api/src/main/operations/%2Faccounts%2F%7BaccountId%7D%2Ftransactions%2F%7BtransactionId%7D/get/response_mapping.yaml:3; Runtime: static-only');
  set(flows.getRow(98), 7, 'Partial');
  set(flows.getRow(98), 6, 'Success returns count/details; controller exceptions may populate MSG-OUT, while JDBC/DB2 failures can be swallowed as null without a response.');

  set(flows.getRow(103), 6, 'Each transaction includes date, time, type, reference, description, and amount; balances appear in the statement summary.');
  set(flows.getRow(119), 4, 'Delete existing CUSTOMER/ACCOUNT/CONTROL rows, then generate and load a requested customer/account range.');
  set(flows.getRow(119), 5, 'The legacy demo utility is destructive and accepts start/end/step/random-seed parameters.');
  set(flows.getRow(119), 10, 'Decisions D-013/D-021: explicit guarded deterministic reset/import replaces destructive startup-style loading.');
  set(flows.getRow(124), 10, 'Decisions D-013/D-021: target import includes legacy transaction-run status through staged atomic promotion.');
  set(flows.getRow(126), 10, 'Decisions D-013/D-021: explicit migrations/import use least-privilege operator credentials, not PUBLIC grants.');

  set(flows.getRow(130), 8, 'legacy/.setup/zconfig/bank-of-z-definitions.yaml:1-1156; legacy/.setup/setup/setup-cics-region.sh:120-190; Runtime: static-only');
  set(flows.getRow(133), 7, 'Partial');
  set(flows.getRow(133), 5, 'GETCOMPY and GETSCODE return literals/configured values, but no business-program callers were found.');
  set(flows.getRow(133), 10, 'Decision D-022: target bank identity is validated configuration; unused helpers are not ported.');
  set(flows.getRow(134), 10, 'Decisions D-006/D-014: target financial/audit writes roll back atomically and emit correlated diagnostics.');
  set(flows.getRow(136), 10, 'Decision D-022: fixed-account diagnostic utilities are not ported as banking capabilities.');
  set(flows.getRow(144), 7, 'Partial');
  set(flows.getRow(144), 6, 'Scan execution publishes results/failures, but missing scan configuration emits a warning and exits successfully.');
  set(flows.getRow(145), 8, 'legacy/.setup/tasks/task-wazi-deploy.sh; legacy/.setup/deploy/deployment-method.yml:104-501; Runtime: static-only');
  set(flows.getRow(150), 10, 'Decision D-022: the broken, uninvoked TOC generator is not ported.');

  for (let n = 7; n <= 153; n += 1) {
    if (epicRows.has(n)) continue;
    const row = flows.getRow(n);
    const current = String(row.getCell(8).value || '');
    if (!/Runtime:\s*(live-observed|simulated|static-only|waived)/i.test(current)) {
      row.getCell(8).value = appendRuntimeLabel(current, 'static-only');
    }
    if (staticOnlyRows.has(n)) row.getCell(8).value = appendRuntimeLabel(row.getCell(8).value, 'static-only');
    for (let column = 1; column <= 14; column += 1) {
      const cell = row.getCell(column);
      const style = JSON.parse(JSON.stringify(cell.style || {}));
      style.font = {
        ...(style.font || {}),
        name: 'Carlito',
        size: 10,
        color: { argb: 'FF000000' },
        bold: column === 2 && String(cell.value || '').trim() !== ''
      };
      const border = { style: 'thin', color: { argb: 'FFE7C8BD' } };
      style.border = { left: border, right: border, top: border, bottom: border };
      style.alignment = {
        ...(style.alignment || {}),
        wrapText: true,
        vertical: 'top',
        horizontal: 'left'
      };
      cell.style = style;
    }
    row.height = undefined;
  }

  flows.pageSetup.printTitlesRow = '1:6';
  rev.pageSetup.printTitlesRow = '1:1';
  rev.eachRow({ includeEmpty: true }, (row) => { row.height = undefined; });

  const decisions = [
    ['R1-D-018', null, '26-27', 'decision', 'Name search is unsupported and failed lookup can retain stale customer state.', 'Target normalized search is implemented; failed lookup clears stale state and disables mutation.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-018'],
    ['R1-D-019', null, '43,45,51', 'decision', 'Channel-specific portfolio paths expose six/ten/twenty-entry limits.', 'Target returns the complete authorized portfolio through bounded pagination.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-019'],
    ['R1-D-020', null, '50,71,80,89', 'decision', 'Sort code is inconsistently operator-entered or supplied internally.', 'Target derives validated sort code from account/bank configuration.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-020'],
    ['R1-D-021', null, '119-126', 'decision', 'Legacy reset/load paths are destructive and can expose partial batch state.', 'Use guarded deterministic reset plus staged atomic import and resumable run ledger.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-021'],
    ['R1-D-022', null, '133,136,150', 'decision', 'Unused helpers and broken/fixed diagnostics are not banking capabilities.', 'Move identity to configuration and do not port these utilities.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-022'],
    ['R1-D-023', null, '33,50,52', 'decision', 'Counter updates and later persistence are not fully proven atomic in source.', 'Target allocation/entity/audit persistence is atomic; identifier gaps are allowed.', 'Yes', 'Yes', 'analysis/stage-04-requirements-revision.md#d-023'],
  ];
  const ids = new Set();
  rev.eachRow((row) => ids.add(String(row.getCell(1).value || '')));
  for (const values of decisions) {
    if (ids.has(values[0])) continue;
    const row = rev.addRow(values);
    const source = rev.getRow(row.number - 1);
    for (let c = 1; c <= 9; c += 1) row.getCell(c).style = JSON.parse(JSON.stringify(source.getCell(c).style || {}));
    row.height = undefined;
  }

  rev.eachRow((row) => {
    const id = String(row.getCell(1).value || '');
    if (id === 'R1-G03') row.getCell(3).value = '41-48,50-58,88,110-112';
    if (id === 'R1-D-020') row.getCell(3).value = '50,71,80,89';
  });

  await workbook.xlsx.writeFile(file);
  console.log('Applied Stage 6 Pass 005 workbook corrections.');
})().catch((error) => {
  console.error(error.stack || error.message);
  process.exit(1);
});
