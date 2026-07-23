import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const tracked = execFileSync('git', ['ls-files', '-z'], { encoding: 'utf8' })
  .split('\0')
  .filter(Boolean)
  .filter(path => !path.startsWith('legacy/'))
  .filter(path => !path.includes('/tests/'))
  .filter(path => !path.includes('/e2e/'))
  .filter(path => !path.endsWith('.md'))
  .filter(path => !path.endsWith('package-lock.json'))
  .filter(path => path !== 'modern/.env.example');

const patterns = [
  {
    name: 'credential assignment',
    expression: /\b[A-Za-z0-9_]*(?:password|token|secret)\b\s*[:=]\s*["']?([^\s"',;}{]{16,})/gi,
    valueIndex: 1
  },
  {
    name: 'connection-string credential',
    expression: /\bPassword\s*=\s*([^;"'\s]{8,})/gi,
    valueIndex: 1
  },
  {
    name: 'private key',
    expression: /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/g,
    valueIndex: 0
  }
];
const allowed =
  /replace-with-|demo-password-123|ci-sql-password-123|validation-[a-z-]+-password-2026|process\.env|environment\.getenvironmentvariable|requesttoken|\$\{|quotename/i;
const findings = [];

for (const path of tracked) {
  const content = readFileSync(path, 'utf8');
  for (const [index, line] of content.split(/\r?\n/).entries()) {
    for (const pattern of patterns) {
      if (path.endsWith('scan-secrets.mjs') && line.includes('expression:')) {
        continue;
      }
      pattern.expression.lastIndex = 0;
      let match;
      while ((match = pattern.expression.exec(line)) !== null) {
        const candidate = pattern.valueIndex === 0 ? match[0] : match[pattern.valueIndex];
        if (!allowed.test(candidate)) {
          findings.push(`${path}:${index + 1} (${pattern.name})`);
        }
      }
    }
  }
}

if (findings.length) {
  console.error(`Potential usable secrets found at: ${findings.join(', ')}`);
  process.exit(1);
}

console.log(`SECRET SCAN OK: ${tracked.length} tracked production/config files checked.`);
