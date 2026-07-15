// Shared helpers for workbook scripts. Two hard-won lessons live here:
// 1) ExcelJS shares style objects between cells — mutate a fill directly and you
//    repaint half the sheet. Always go through setFill (clones the style first).
// 2) Cell values may be rich text or hyperlink objects — always read via cellText.
'use strict';

const COLORS = {
  GREEN: 'FFE2F0D9',   // done
  ORANGE: 'FFFCE4D6',  // deferred by decision (M=Yes + reason)
  RED: 'FFFFC7CE',     // not done / unclassified — actionable
  GRAY: 'FFEDEDED',    // decision proposal (revision sheets)
};

function cellText(cell) {
  const x = cell.value;
  if (x == null) return '';
  if (typeof x === 'object' && x.richText) return x.richText.map((t) => t.text).join('');
  if (typeof x === 'object' && x.text != null) return String(x.text);
  return String(x);
}

function fillArgb(cell) {
  const f = cell.fill;
  return (f && f.fgColor && f.fgColor.argb) || null;
}

/** Set a fill without corrupting shared styles: clone the style, then assign. */
function setFill(cell, argb) {
  const st = JSON.parse(JSON.stringify(cell.style || {}));
  st.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb } };
  cell.style = st;
}

/** Parse row references like "94", "70-72", "232, 236, 238", "стр. 70–72" into row numbers. */
function parseRefs(ref) {
  const out = [];
  const clean = String(ref).replace(/стр\.\s*/g, '').replace(/–/g, '-').trim();
  for (const part of clean.split(',')) {
    const m = part.trim().match(/^(\d+)(?:-(\d+))?$/);
    if (m) {
      const a = Number(m[1]);
      const b = m[2] ? Number(m[2]) : a;
      for (let x = a; x <= b; x++) out.push(x);
    }
  }
  return out;
}

/** Snapshot values + fills of a worksheet (for before/after integrity audits). */
function snapshot(ws, maxCol = 15) {
  const s = new Map();
  ws.eachRow({ includeEmpty: true }, (row, r) => {
    for (let c = 1; c <= maxCol; c++) {
      s.set(r + ':' + c, JSON.stringify(row.getCell(c).value ?? null) + '|' + (fillArgb(row.getCell(c)) || '-'));
    }
  });
  return s;
}

/** Diff a worksheet against a snapshot; returns keys that differ. */
function diffAgainst(ws, snap, maxCol = 15) {
  const diffs = [];
  ws.eachRow({ includeEmpty: true }, (row, r) => {
    for (let c = 1; c <= maxCol; c++) {
      const key = r + ':' + c;
      const now = JSON.stringify(row.getCell(c).value ?? null) + '|' + (fillArgb(row.getCell(c)) || '-');
      if (now !== (snap.get(key) ?? 'null|-')) diffs.push(key);
    }
  });
  return diffs;
}

module.exports = { COLORS, cellText, fillArgb, setFill, parseRefs, snapshot, diffAgainst };
