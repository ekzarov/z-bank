import { readFileSync } from 'node:fs';

const read = path => readFileSync(new URL(path, import.meta.url), 'utf8');
const compose = read('../compose.yaml');
const apiDockerfile = read('../Dockerfile');
const uiDockerfile = read('../frontend/bank-of-z-ui/Dockerfile');
const nginx = read('../frontend/bank-of-z-ui/nginx.conf');
const serviceBlock = name => compose.match(new RegExp(`\\n  ${name}:\\n([\\s\\S]*?)(?=\\n  [a-z][a-z0-9-]*:\\n|\\nvolumes:)`))?.[1] ?? '';
const api = serviceBlock('api');
const db = serviceBlock('db');

const checks = [
  ['API is non-root', apiDockerfile.includes('USER app')],
  ['UI is non-root', uiDockerfile.includes('nginx-unprivileged')],
  ['API and SQL are internal-only', !api.includes('ports:') && !db.includes('ports:')],
  ['database network is internal', compose.includes('internal: true')],
  ['resource limits exist', compose.includes('resources:') && compose.includes('memory: 512M')],
  ['graceful shutdown is configured', compose.includes('stop_grace_period')],
  ['API readiness is the container gate', compose.includes('GET /health/ready')],
  ['startup has no setup dependency', !api.match(/depends_on:[\s\S]*?setup:/)],
  ['nginx request limit exists', nginx.includes('client_max_body_size 11m')],
  ['nginx security headers exist', nginx.includes('Content-Security-Policy') && nginx.includes('X-Content-Type-Options')],
  ['nginx forwards correlation', nginx.includes('X-Correlation-ID')]
];

const failures = checks.filter(([, passed]) => !passed).map(([name]) => name);
if (failures.length) {
  console.error(`DELIVERY AUDIT FAILED: ${failures.join('; ')}`);
  process.exit(1);
}

console.log(`DELIVERY AUDIT OK: ${checks.length} controls verified.`);
