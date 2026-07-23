import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const tracked = execFileSync('git', ['ls-files', '-z'], { encoding: 'utf8' })
  .split('\0')
  .filter(Boolean)
  .filter(path => !path.startsWith('legacy/'))
  .filter(path => !path.startsWith('.github/workflows/'))
  .filter(path => !path.includes('/tests/'))
  .filter(path => !path.includes('/e2e/'))
  .filter(path => !path.endsWith('.md'))
  .filter(path => !path.endsWith('package-lock.json'))
  .filter(path => path !== 'modern/.env.example');

const assignment = /(password|token|secret)\w*\s*[:=]\s*["']([^"']{16,})["']/gi;
const allowed = /replace-with-|demo-password-123|process\.env|environment\.getenvironmentvariable|\$\{|quotename/i;
const findings = [];

for (const path of tracked) {
  const content = readFileSync(path, 'utf8');
  for (const [index, line] of content.split(/\r?\n/).entries()) {
    assignment.lastIndex = 0;
    let match;
    while ((match = assignment.exec(line)) !== null) {
      if (!allowed.test(match[2])) findings.push(`${path}:${index + 1}`);
    }
  }
}

if (findings.length) {
  console.error(`Potential usable secrets found at: ${findings.join(', ')}`);
  process.exit(1);
}

console.log(`SECRET SCAN OK: ${tracked.length} tracked production/config files checked.`);
