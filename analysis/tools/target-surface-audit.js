'use strict';

const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..', '..');

function normalizeRoute(route) {
  if (route === '') return '/';
  if (route === '**') return '/**';
  return route.startsWith('/') ? route : `/${route}`;
}

function readRoutes(file) {
  const source = fs.readFileSync(file, 'utf8');
  return [...source.matchAll(/\{\s*path:\s*'([^']*)'/g)]
    .map((match) => normalizeRoute(match[1]));
}

function readNavigation(file) {
  const source = fs.readFileSync(file, 'utf8');
  return [...source.matchAll(/\{\s*path:\s*'([^']+)'\s*,\s*label:\s*'([^']+)'(?:\s*,\s*role:\s*'([^']+)')?\s*\}/g)]
    .map((match) => ({ path: normalizeRoute(match[1]), label: match[2], role: match[3] || null }));
}

function walkFiles(directory) {
  const files = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const target = path.join(directory, entry.name);
    if (entry.isDirectory()) files.push(...walkFiles(target));
    else if (/\.(?:html|ts|tsx|js|jsx)$/.test(entry.name) && !/\.spec\.[^.]+$/.test(entry.name)) files.push(target);
  }
  return files;
}

function findMarkers(frontendRoot, markers) {
  const findings = [];
  for (const file of walkFiles(frontendRoot)) {
    const source = fs.readFileSync(file, 'utf8').toLowerCase();
    for (const marker of markers) {
      if (source.includes(marker.toLowerCase())) {
        findings.push(`${path.relative(root, file)} contains visible placeholder marker "${marker}"`);
      }
    }
  }
  return findings;
}

function validateReferences(inventory, errors) {
  for (const surface of inventory.surfaces) {
    for (const action of surface.actions || []) {
      for (const field of ['requirements', 'code', 'tests']) {
        if (!Array.isArray(action[field]) || action[field].length === 0) {
          errors.push(`${surface.path} action "${action.name || '<unnamed>'}" has no ${field} evidence`);
          continue;
        }
        for (const reference of action[field]) {
          const [relativeFile, anchor] = reference.split('#', 2);
          if (field === 'tests' && !anchor) {
            errors.push(`${surface.path} test evidence ${reference} must name the exact test after #`);
            continue;
          }
          const absoluteFile = path.join(root, relativeFile);
          if (!fs.existsSync(absoluteFile)) {
            errors.push(`${surface.path} references missing ${relativeFile}`);
          } else if (anchor && !fs.readFileSync(absoluteFile, 'utf8').includes(anchor)) {
            errors.push(`${surface.path} references missing ${anchor} in ${relativeFile}`);
          }
        }
      }
    }
  }
}

function validateModel({ inventory, routePaths, navigationItems, markerFindings, checkReferences = false }) {
  const errors = [...markerFindings];
  const surfacesByPath = new Map();

  for (const surface of inventory.surfaces || []) {
    if (!surface.id || !surface.path || !Array.isArray(surface.roles) || surface.roles.length === 0) {
      errors.push(`Invalid target surface entry ${JSON.stringify(surface)}`);
      continue;
    }
    if (surfacesByPath.has(surface.path)) errors.push(`Duplicate target surface path ${surface.path}`);
    surfacesByPath.set(surface.path, surface);

    if (surface.status === 'implemented' && (!Array.isArray(surface.actions) || surface.actions.length === 0)) {
      errors.push(`${surface.path} is implemented but has no useful action or observable contract`);
    }
    if (surface.status === 'gap' && surface.visibility !== 'hidden') {
      errors.push(`${surface.path} is a visible unresolved gap`);
    }
    if (surface.status === 'deferred' && surface.visibility !== 'hidden') {
      errors.push(`${surface.path} is deferred but remains visible`);
    }
    if (!['implemented', 'gap', 'deferred'].includes(surface.status)) {
      errors.push(`${surface.path} has unsupported status ${surface.status}`);
    }
  }

  for (const routePath of routePaths) {
    if (!surfacesByPath.has(routePath)) errors.push(`Angular route ${routePath} is absent from target-surface inventory`);
  }
  for (const surfacePath of surfacesByPath.keys()) {
    if (!routePaths.includes(surfacePath)) errors.push(`Target-surface path ${surfacePath} is absent from Angular routes`);
  }

  const roleSpecificImplemented = new Set();
  for (const item of navigationItems) {
    const surface = surfacesByPath.get(item.path);
    if (!surface) {
      errors.push(`Navigation item ${item.path} is absent from target-surface inventory`);
      continue;
    }
    if (surface.status !== 'implemented') {
      errors.push(`Navigation item ${item.path} points to ${surface.status} surface`);
    }
    if (item.role && surface.status === 'implemented' && (surface.actions || []).length > 0) {
      roleSpecificImplemented.add(item.role);
    }
  }
  for (const role of new Set(navigationItems.map((item) => item.role).filter(Boolean))) {
    if (!roleSpecificImplemented.has(role)) {
      errors.push(`Role ${role} has no implemented role-specific navigation action`);
    }
  }

  if (checkReferences) validateReferences(inventory, errors);
  return errors;
}

function run() {
  const inventoryFile = path.join(root, 'analysis', 'target-surface-inventory.json');
  const inventory = JSON.parse(fs.readFileSync(inventoryFile, 'utf8'));
  const routeFile = path.join(root, inventory.routeFile);
  const navigationFile = path.join(root, inventory.navigationFile);
  const frontendRoot = path.join(root, inventory.frontendRoot);
  const routePaths = readRoutes(routeFile);
  const navigationItems = readNavigation(navigationFile);
  const markerFindings = findMarkers(frontendRoot, inventory.forbiddenVisibleMarkers || []);
  const errors = validateModel({
    inventory,
    routePaths,
    navigationItems,
    markerFindings,
    checkReferences: true
  });

  if (errors.length) {
    errors.forEach((error) => console.error(`FAIL ${error}`));
    console.error(`TARGET SURFACE AUDIT FAILED: ${errors.length} violation(s)`);
    process.exit(1);
  }

  console.log(`TARGET SURFACE AUDIT OK: ${inventory.surfaces.length} routes, ${navigationItems.length} navigation items`);
}

if (require.main === module) run();

module.exports = { normalizeRoute, validateModel };
