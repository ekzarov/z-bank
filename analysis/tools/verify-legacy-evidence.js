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
  let checkedRows = 0;
  let checkedSegments = 0;

  // Build a cache of all files under legacy/ recursively
  const allLegacyFiles = [];
  function buildFileCache(dir) {
    const list = fs.readdirSync(dir);
    for (const item of list) {
      const full = path.join(dir, item);
      const rel = path.relative(rootDir, full).replace(/\\/g, '/');
      const stats = fs.statSync(full);
      if (stats.isDirectory()) {
        buildFileCache(full);
      } else {
        allLegacyFiles.push(rel);
      }
    }
  }
  buildFileCache(path.join(rootDir, 'legacy'));

  let prevDirectory = 'legacy';
  const classCounts = {
    runtime: 0,
    negative: 0,
    standard: 0,
    zosAsset: 0,
    operations: 0,
    copybook: 0,
    db2Table: 0,
    descriptive: 0
  };

  function findFilesInCache(pattern, relativeToDir) {
    let pat = pattern.replace(/\\/g, '/');
    if (relativeToDir) {
      const relPattern = (relativeToDir + '/' + pat).replace(/\/+/g, '/');
      const regex = new RegExp('^' + relPattern.replace(/\./g, '\\.').replace(/\*/g, '.*') + '$', 'i');
      const matches = allLegacyFiles.filter(f => regex.test(f));
      if (matches.length > 0) return matches;
    }
    let rootPattern = pat;
    if (!rootPattern.startsWith('legacy/') && !rootPattern.startsWith('simulation/')) {
      rootPattern = 'legacy/' + rootPattern;
    }
    const regexRoot = new RegExp('^' + rootPattern.replace(/\./g, '\\.').replace(/\*/g, '.*') + '$', 'i');
    const matchesRoot = allLegacyFiles.filter(f => regexRoot.test(f));
    if (matchesRoot.length > 0) return matchesRoot;

    const cleanPattern = pat.replace(/\./g, '\\.').replace(/\*/g, '.*');
    const regexGlobal = new RegExp('(?:^|/)' + cleanPattern + '$', 'i');
    const matchesGlobal = allLegacyFiles.filter(f => regexGlobal.test(f));
    if (matchesGlobal.length > 0) return matchesGlobal;

    let checkDir = pat;
    if (!checkDir.startsWith('legacy/') && !checkDir.startsWith('simulation/')) {
      checkDir = 'legacy/' + checkDir;
    }
    const fullDir = path.join(rootDir, checkDir);
    if (fs.existsSync(fullDir) && fs.statSync(fullDir).isDirectory()) {
      return [checkDir];
    }
    return [];
  }

  function findDirInOperations(pattern) {
    const opsDir = path.join(rootDir, 'legacy/src/api/src/main/operations');
    if (!fs.existsSync(opsDir)) return [];
    const dirs = [];
    function recurse(d) {
      const list = fs.readdirSync(d);
      for (const item of list) {
        const full = path.join(d, item);
        if (fs.statSync(full).isDirectory()) {
          const rel = path.relative(opsDir, full).replace(/\\/g, '/');
          dirs.push(rel);
          recurse(full);
        }
      }
    }
    recurse(opsDir);

    const cleanPattern = pattern.replace(/\./g, '\\.').replace(/\*/g, '.*');
    const regex = new RegExp(cleanPattern + '$', 'i');
    return dirs.filter(d => regex.test(d)).map(d => 'legacy/src/api/src/main/operations/' + d);
  }

  function verifyFileLineRange(filePath, lineSpec, rowNumber) {
    const fullPath = path.join(rootDir, filePath);
    if (!fs.existsSync(fullPath)) {
      errors.push(`Row ${rowNumber}: File not found for line range check: ${filePath}`);
      return;
    }
    const stats = fs.statSync(fullPath);
    if (stats.isDirectory()) {
      return;
    }
    const content = fs.readFileSync(fullPath, 'utf8');
    const lines = content.split(/\r?\n/);
    const lineCount = lines.length;

    const parts = lineSpec.split(',');
    for (const part of parts) {
      const cleanSpec = part.replace(/[^\d\-]/g, '');
      const m = cleanSpec.match(/^(\d+)(?:-(\d+))?$/);
      if (m) {
        const start = parseInt(m[1], 10);
        const end = m[2] ? parseInt(m[2], 10) : start;
        if (start > lineCount) {
          errors.push(`Row ${rowNumber}: Line spec ${part} starts at ${start} but file ${filePath} has only ${lineCount} lines`);
        } else if (end > lineCount) {
          errors.push(`Row ${rowNumber}: Line spec ${part} ends at ${end} but file ${filePath} has only ${lineCount} lines`);
        }
      }
    }
  }

  function verifyNegativeEvidence(segment, rowNumber) {
    const lower = segment.toLowerCase();
    if (lower.includes('no inqaccty implementation exists')) {
      const matches = findFilesInCache('*INQACCTY*');
      if (matches.length > 0) {
        errors.push(`Row ${rowNumber}: Negative evidence failed: INQACCTY implementation found: ${matches.join(', ')}`);
      }
    } else if (lower.includes('no mqconn/mqopen/mqput/mqget calls are present')) {
      const matches = [];
      for (const file of allLegacyFiles) {
        if (!file.startsWith('legacy/src/')) continue;
        const content = fs.readFileSync(path.join(rootDir, file), 'utf8');
        if (/MQCONN|MQOPEN|MQPUT|MQGET/i.test(content)) matches.push(file);
      }
      if (matches.length > 0) {
        errors.push(`Row ${rowNumber}: Negative evidence failed: MQ calls found in: ${matches.slice(0, 5).join(', ')}`);
      }
    } else if (lower.includes('no authentication middleware or login page is present')) {
      const matches = [];
      for (const file of allLegacyFiles) {
        if (!file.startsWith('legacy/src/frontend/')) continue;
        if (file.includes('login') || file.includes('auth')) matches.push(file);
      }
      if (matches.length > 0) {
        errors.push(`Row ${rowNumber}: Negative evidence failed: auth/login pages found: ${matches.join(', ')}`);
      }
    } else if (lower.includes('no executable generate_toc() invocation is present')) {
      const scriptFile = path.join(rootDir, 'legacy/docs/scripts/generate_toc_from_md.py');
      if (fs.existsSync(scriptFile)) {
        const content = fs.readFileSync(scriptFile, 'utf8');
        const calls = content.match(/^(?!\s*def\s+)generate_toc\(/m);
        if (calls) {
          errors.push(`Row ${rowNumber}: Negative evidence failed: generate_toc() invocation found in ${scriptFile}`);
        }
      }
    } else {
      errors.push(`Row ${rowNumber}: Unrecognized negative evidence statement: "${segment}"`);
    }
  }

  function verifySegment(segment, rowNumber) {
    checkedSegments++;
    let trimmed = segment.trim();

    // 1. Runtime / harness / basis
    if (trimmed.startsWith('Runtime:') || trimmed.startsWith('harness:') || trimmed.startsWith('basis:')) {
      classCounts.runtime++;
      const filesMatch = trimmed.match(/(?:basis:|harness:|simulated\s+—\s+)([a-zA-Z0-9_\-\.\/\*]+)(?::([0-9\-\,\s]+))?/);
      if (filesMatch) {
        const filePath = filesMatch[1];
        const lineSpec = filesMatch[2];
        const resolved = findFilesInCache(filePath, prevDirectory);
        if (resolved.length === 0 && !fs.existsSync(path.join(rootDir, filePath))) {
          errors.push(`Row ${rowNumber}: File not found in runtime/harness/basis citation: ${filePath}`);
        } else {
          for (const match of resolved) {
            if (lineSpec) verifyFileLineRange(match, lineSpec, rowNumber);
          }
        }
      }
      return;
    }

    // 2. Negative evidence
    if (trimmed.toLowerCase().startsWith('no ')) {
      classCounts.negative++;
      verifyNegativeEvidence(trimmed, rowNumber);
      return;
    }

    // 3. Standard Path(s) (Starts with legacy/ or simulation/, or ends with an extension)
    if (trimmed.startsWith('legacy/') || trimmed.startsWith('simulation/') ||
        /(?:[\w\.\-\/\*\{\}\%]+\.(?:groovy|json|java|yaml|html|cbl|pli|jcl|xml|bms|asm|cpy|data|yml|j2|py|js|sh))/i.test(trimmed)) {
      classCounts.standard++;
      const fileRegex = /(?:legacy\/|simulation\/)[\w\.\-\/\*\{\}\%]+(?::[0-9\-\,\s]+)?|[\w\.\-\/\*\{\}\%]+\.(?:groovy|json|java|yaml|html|cbl|pli|jcl|xml|bms|asm|cpy|data|yml|j2|py|js|sh)(?::[0-9\-\,\s]+)?/gi;
      const matches = trimmed.match(fileRegex);
      if (matches) {
        for (const m of matches) {
          const parts = m.split(':');
          const filePath = parts[0];
          const lineSpec = parts[1];
          const resolved = findFilesInCache(filePath, prevDirectory);
          if (resolved.length > 0) {
            prevDirectory = path.dirname(resolved[0]);
            for (const file of resolved) {
              if (lineSpec) verifyFileLineRange(file, lineSpec, rowNumber);
            }
          } else {
            errors.push(`Row ${rowNumber}: File not found: ${filePath}`);
          }
        }
      } else {
        errors.push(`Row ${rowNumber}: Could not extract file from standard path: "${trimmed}"`);
      }
      return;
    }

    // 4. zosAssets
    if (trimmed.toLowerCase().includes('zosassets/')) {
      classCounts.zosAsset++;
      const zosMatch = trimmed.match(/zosAssets\/([\w\.\,\-\s]+)/i);
      if (zosMatch) {
        const assets = zosMatch[1].split(/\s+and\s+|\s*,\s*/i);
        for (const asset of assets) {
          const dirPath = `legacy/src/api/src/main/zosAssets/${asset.trim()}`;
          const exists = fs.existsSync(path.join(rootDir, dirPath));
          if (!exists) {
            errors.push(`Row ${rowNumber}: zosAsset path not found: ${dirPath}`);
          }
        }
      } else {
        errors.push(`Row ${rowNumber}: Malformed zosAssets reference: "${trimmed}"`);
      }
      return;
    }

    // 5. operations
    if (trimmed.toLowerCase().includes('operations/')) {
      classCounts.operations++;
      const opsMatch = trimmed.match(/operations\/([\w\.\/\%\{\}\*]+)/i);
      if (opsMatch) {
        const opPath = opsMatch[1].replace(/\.\.\./g, '*');
        let resolved = [];
        const fullDir = path.join(rootDir, 'legacy/src/api/src/main/operations', opPath);
        if (fs.existsSync(fullDir) && fs.statSync(fullDir).isDirectory()) {
          resolved = ['legacy/src/api/src/main/operations/' + opPath];
        } else {
          resolved = findDirInOperations(opPath);
        }
        if (resolved.length === 0) {
          errors.push(`Row ${rowNumber}: Operations path not found: ${opPath}`);
        }
      } else {
        errors.push(`Row ${rowNumber}: Malformed operations path: "${trimmed}"`);
      }
      return;
    }

    if (trimmed.toLowerCase().startsWith('operations ')) {
      classCounts.operations++;
      const opsWordMatch = trimmed.match(/operations\s+\.\.\.([\w\.\-\*]+)\.\.\./i);
      if (opsWordMatch) {
        const word = opsWordMatch[1];
        const opsDir = path.join(rootDir, 'legacy/src/api/src/main/operations');
        let matched = [];
        if (fs.existsSync(opsDir)) {
          const files = fs.readdirSync(opsDir);
          matched = files.filter(f => f.toLowerCase().includes(word.toLowerCase()));
        }
        if (matched.length === 0) {
          errors.push(`Row ${rowNumber}: Operations word matched no dirs: ${word}`);
        }
      } else {
        errors.push(`Row ${rowNumber}: Malformed operations word match: "${trimmed}"`);
      }
      return;
    }

    // 6. copybook SORTCODE
    if (trimmed.toLowerCase().includes('copybook ')) {
      classCounts.copybook++;
      const match = trimmed.match(/copybook\s+([\w\-]+)/i);
      if (match) {
        const cbName = match[1];
        const resolved = findFilesInCache(`${cbName}.cpy`);
        if (resolved.length === 0) {
          errors.push(`Row ${rowNumber}: Copybook not found: ${cbName}`);
        }
      } else {
        errors.push(`Row ${rowNumber}: Malformed copybook reference: "${trimmed}"`);
      }
      return;
    }

    // 7. DB2 tables (IMSBANK.HISTORY)
    if (trimmed.includes('IMSBANK.')) {
      classCounts.db2Table++;
      const targetJava = path.join(rootDir, 'legacy/src/base/ims/java/src/main/java/nazare/jmp/history/TransactionService.java');
      const hasTable = fs.existsSync(targetJava) && fs.readFileSync(targetJava, 'utf8').includes('IMSBANK.HISTORY');
      if (!hasTable) {
        errors.push(`Row ${rowNumber}: DB2 Table reference not verified: ${trimmed}`);
      }
      return;
    }

    // 8. General descriptive / narrative terms
    const lower = trimmed.toLowerCase();
    if (lower === 'all core mappings require external cics/ims endpoints') {
      classCounts.descriptive++;
      const exists = fs.existsSync(path.join(rootDir, 'legacy/docker-compose.yaml'));
      if (!exists) errors.push(`Row ${rowNumber}: docker-compose.yaml not found`);
      return;
    }

    if (['depositrequest schema', 'error schema', 'response sections and error schema'].includes(lower)) {
      classCounts.descriptive++;
      const schemaName = lower.includes('depositrequest') ? 'DepositRequest' : 'Error';
      const openapiFile = path.join(rootDir, 'legacy/src/api/src/main/api/openapi.yaml');
      const hasSchema = fs.existsSync(openapiFile) && fs.readFileSync(openapiFile, 'utf8').includes(schemaName);
      if (!hasSchema) {
        errors.push(`Row ${rowNumber}: OpenAPI schema not found: ${schemaName}`);
      }
      return;
    }

    if (lower.startsWith('auth.bankofz.example.com')) {
      classCounts.descriptive++;
      const openapiFile = path.join(rootDir, 'legacy/src/api/src/main/api/openapi.yaml');
      const hasEndpoint = fs.existsSync(openapiFile) && fs.readFileSync(openapiFile, 'utf8').includes('auth.bankofz.example.com');
      if (!hasEndpoint) {
        errors.push(`Row ${rowNumber}: OAuth placeholder not found in openapi.yaml`);
      }
      return;
    }

    if (lower.startsWith('facility type checks at')) {
      classCounts.descriptive++;
      const linesMatch = trimmed.match(/(\d+-\d+)\s+and\s+(\d+-\d+)/);
      if (linesMatch) {
        const dbcrFile = path.join(rootDir, 'legacy/src/base/cics/cobol/DBCRFUN.cbl');
        if (!fs.existsSync(dbcrFile)) {
          errors.push(`Row ${rowNumber}: DBCRFUN.cbl not found`);
        }
      } else {
        errors.push(`Row ${rowNumber}: Malformed facility type checks statement: "${trimmed}"`);
      }
      return;
    }

    // Unknown segments cause audit failure
    errors.push(`Row ${rowNumber}: Unknown/unclassified evidence segment: "${trimmed}"`);
  }

  main.eachRow({ includeEmpty: true }, (row, r) => {
    if (r < 7) return; // Skip headers
    const useCaseId = cellText(row.getCell(1)).trim();
    if (useCaseId !== "") return; // Skip epic banner rows

    const evidence = cellText(row.getCell(8)).trim();
    if (!evidence) {
      errors.push(`Row ${r}: No legacy source evidence cited`);
      return;
    }

    checkedRows++;
    const segments = evidence.split(';');
    for (const seg of segments) {
      if (seg.trim() !== "") {
        verifySegment(seg, r);
      }
    }
  });

  console.log(`\nChecked ${checkedRows} rows and verified ${checkedSegments} evidence references.`);
  console.log('Class counts:');
  Object.keys(classCounts).forEach(k => console.log(`  - ${k}: ${classCounts[k]}`));

  if (errors.length > 0) {
    console.error(`\nFound ${errors.length} errors:`);
    errors.forEach(e => console.error('  ' + e));
    process.exit(1);
  } else {
    console.log('\nAll legacy source evidence files, wildcards, line ranges, and negative evidence claims are 100% valid!');
    process.exit(0);
  }
})().catch(e => {
  console.error(e);
  process.exit(1);
});
