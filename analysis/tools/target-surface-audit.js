'use strict';

const fs = require('fs');
const path = require('path');
const ts = require('typescript');

const root = path.resolve(__dirname, '..', '..');

function normalizeRoute(route) {
  if (route === '') return '/';
  if (route === '**') return '/**';
  return route.startsWith('/') ? route : `/${route}`;
}

function joinRoute(parent, child) {
  if (child === '**') return '/**';
  const segments = [parent, child]
    .flatMap((part) => part.split('/'))
    .filter(Boolean);
  return segments.length ? `/${segments.join('/')}` : '/';
}

function parseSource(file) {
  return ts.createSourceFile(
    file,
    fs.readFileSync(file, 'utf8'),
    ts.ScriptTarget.Latest,
    true,
    ts.ScriptKind.TS
  );
}

function property(object, name) {
  return object.properties.find((candidate) =>
    ts.isPropertyAssignment(candidate) &&
    ((ts.isIdentifier(candidate.name) && candidate.name.text === name) ||
      (ts.isStringLiteral(candidate.name) && candidate.name.text === name)));
}

function literalValue(assignment) {
  return assignment && ts.isStringLiteralLike(assignment.initializer)
    ? assignment.initializer.text
    : null;
}

function variableArray(sourceFile, name, errors) {
  let result = null;
  sourceFile.forEachChild((node) => {
    if (!ts.isVariableStatement(node)) return;
    for (const declaration of node.declarationList.declarations) {
      if (ts.isIdentifier(declaration.name) &&
          declaration.name.text === name &&
          declaration.initializer &&
          ts.isArrayLiteralExpression(declaration.initializer)) {
        result = declaration.initializer;
      }
    }
  });
  if (!result) errors.push(`Cannot find array variable ${name} in ${sourceFile.fileName}`);
  return result;
}

function readRoutes(file) {
  const errors = [];
  const sourceFile = parseSource(file);
  const routesArray = variableArray(sourceFile, 'routes', errors);
  const routes = [];

  function visitRoutes(array, parentPath = '/') {
    for (const element of array.elements) {
      if (!ts.isObjectLiteralExpression(element)) {
        errors.push(`Unsupported non-object route in ${file}`);
        continue;
      }
      const routePath = literalValue(property(element, 'path'));
      if (routePath === null) {
        errors.push(`Route path must be a string literal in ${file}`);
        continue;
      }
      const normalizedPath = joinRoute(parentPath, routePath);
      const dataProperty = property(element, 'data');
      let role = null;
      if (dataProperty && ts.isObjectLiteralExpression(dataProperty.initializer)) {
        role = literalValue(property(dataProperty.initializer, 'role'));
      }
      routes.push({ path: normalizedPath, role });

      const childrenProperty = property(element, 'children');
      if (childrenProperty && ts.isArrayLiteralExpression(childrenProperty.initializer)) {
        visitRoutes(childrenProperty.initializer, normalizedPath);
      }
    }
  }

  if (routesArray) visitRoutes(routesArray);
  return { routes, errors };
}

function readNavigation(file) {
  const errors = [];
  const sourceFile = parseSource(file);
  const itemsArray = variableArray(sourceFile, 'items', errors);
  const items = [];

  if (itemsArray) {
    for (const element of itemsArray.elements) {
      if (!ts.isObjectLiteralExpression(element)) {
        errors.push(`Unsupported non-object navigation item in ${file}`);
        continue;
      }
      const itemPath = literalValue(property(element, 'path'));
      const label = literalValue(property(element, 'label'));
      const role = literalValue(property(element, 'role'));
      if (itemPath === null || label === null) {
        errors.push(`Navigation path and label must be string literals in ${file}`);
        continue;
      }
      items.push({ path: normalizeRoute(itemPath), label, role });
    }
  }
  return { items, errors };
}

function walkFiles(directory) {
  const files = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const target = path.join(directory, entry.name);
    if (entry.isDirectory()) files.push(...walkFiles(target));
    else if (/\.html$/.test(entry.name) || (/\.ts$/.test(entry.name) && !/\.spec\.ts$/.test(entry.name))) files.push(target);
  }
  return files;
}

function renderedText(file) {
  if (file.endsWith('.html')) return fs.readFileSync(file, 'utf8');
  const sourceFile = parseSource(file);
  const templates = [];
  function visit(node) {
    if (ts.isDecorator(node) &&
        ts.isCallExpression(node.expression) &&
        node.expression.expression.getText(sourceFile) === 'Component') {
      const metadata = node.expression.arguments[0];
      if (metadata && ts.isObjectLiteralExpression(metadata)) {
        const template = property(metadata, 'template');
        if (template && ts.isStringLiteralLike(template.initializer)) {
          templates.push(template.initializer.text);
        }
      }
    }
    ts.forEachChild(node, visit);
  }
  visit(sourceFile);
  return templates.join('\n');
}

function findMarkers(frontendRoot, markers) {
  const findings = [];
  for (const file of walkFiles(frontendRoot)) {
    const source = renderedText(file).toLowerCase();
    for (const marker of markers) {
      if (source.includes(marker.toLowerCase())) {
        findings.push(`${path.relative(root, file)} contains visible placeholder marker "${marker}"`);
        break;
      }
    }
  }
  return findings;
}

function validateReferences(inventory, errors) {
  for (const surface of inventory.surfaces) {
    for (const action of surface.actions || []) {
      for (const field of ['requirements', 'code']) {
        if (!Array.isArray(action[field]) || action[field].length === 0) {
          errors.push(`${surface.path} action "${action.name || '<unnamed>'}" has no ${field} evidence`);
          continue;
        }
        for (const reference of action[field]) validateFileReference(surface, reference, errors);
      }

      if (!Array.isArray(action.tests) || action.tests.length === 0) {
        errors.push(`${surface.path} action "${action.name || '<unnamed>'}" has no tests evidence`);
        continue;
      }
      const actionRoles = new Set(action.roles || []);
      for (const test of action.tests) {
        if (!test || typeof test.reference !== 'string' || !Array.isArray(test.roles) || test.roles.length === 0) {
          errors.push(`${surface.path} has malformed role-bound test evidence`);
          continue;
        }
        const [relativeFile, anchor] = test.reference.split('#', 2);
        if (!anchor) {
          errors.push(`${surface.path} test evidence ${test.reference} must name the exact test after #`);
          continue;
        }
        for (const role of test.roles) {
          if (!actionRoles.has(role)) {
            errors.push(`${surface.path} test assigns undeclared action role ${role}`);
          }
        }
        const absoluteFile = path.join(root, relativeFile);
        if (!fs.existsSync(absoluteFile)) {
          errors.push(`${surface.path} references missing ${relativeFile}`);
          continue;
        }
        const testTitles = readTestTitles(absoluteFile);
        const matchingTitles = testTitles.filter((title) => title.includes(anchor));
        if (matchingTitles.length === 0) {
          errors.push(`${surface.path} references missing ${anchor} in ${relativeFile}`);
          continue;
        }
        if (!matchingTitles.some((title) => title.includes(`@surface:${surface.id}`))) {
          errors.push(`${surface.path} test ${relativeFile} lacks @surface:${surface.id}`);
        }
        for (const role of test.roles) {
          if (!matchingTitles.some((title) =>
            title.includes(`@surface:${surface.id}`) && title.includes(`@role:${role}`))) {
            errors.push(`${surface.path} test ${relativeFile} lacks @role:${role}`);
          }
        }
      }
      for (const role of actionRoles) {
        if (!action.tests.some((test) => test.roles && test.roles.includes(role))) {
          errors.push(`${surface.path} action "${action.name}" has no test bound to role ${role}`);
        }
      }
    }
  }
}

function readTestTitles(file) {
  const sourceFile = parseSource(file);
  const titles = [];
  function visit(node) {
    if (ts.isCallExpression(node) &&
        (node.expression.getText(sourceFile) === 'test' || node.expression.getText(sourceFile) === 'it')) {
      const title = node.arguments[0];
      if (title && ts.isStringLiteralLike(title)) titles.push(title.text);
    }
    ts.forEachChild(node, visit);
  }
  visit(sourceFile);
  return titles;
}

function validateFileReference(surface, reference, errors) {
  const [relativeFile, anchor] = reference.split('#', 2);
  const absoluteFile = path.join(root, relativeFile);
  if (!fs.existsSync(absoluteFile)) {
    errors.push(`${surface.path} references missing ${relativeFile}`);
  } else if (anchor && !fs.readFileSync(absoluteFile, 'utf8').includes(anchor)) {
    errors.push(`${surface.path} references missing ${anchor} in ${relativeFile}`);
  }
}

function validateModel({
  inventory,
  routes,
  navigationItems,
  markerFindings,
  parserFindings = [],
  checkReferences = false
}) {
  const errors = [...parserFindings, ...markerFindings];
  const surfacesByPath = new Map();
  const routePaths = routes.map((route) => route.path);

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

    const declaredActionRoles = new Set((surface.actions || []).flatMap((action) => action.roles || []));
    const requiredRoles = surface.roleInvariant ? ['Shared'] : surface.roles;
    for (const role of requiredRoles) {
      if (surface.status === 'implemented' && !declaredActionRoles.has(role)) {
        errors.push(`${surface.path} has no useful action assigned to role ${role}`);
      }
    }
  }

  for (const route of routes) {
    const surface = surfacesByPath.get(route.path);
    if (!surface) {
      errors.push(`Angular route ${route.path} is absent from target-surface inventory`);
      continue;
    }
    if (surface.visibility === 'hidden' || surface.status === 'deferred') {
      errors.push(`${route.path} is declared hidden/deferred but remains reachable as an Angular route`);
    }
    if (route.role && !surface.roles.includes(route.role)) {
      errors.push(`${route.path} route guard role ${route.role} is absent from inventory roles`);
    }
  }
  for (const surface of surfacesByPath.values()) {
    const mayBeAbsent = surface.visibility === 'hidden' &&
      (surface.status === 'deferred' || surface.status === 'gap');
    if (!mayBeAbsent && !routePaths.includes(surface.path)) {
      errors.push(`Target-surface path ${surface.path} is absent from Angular routes`);
    }
  }

  const exposedRoles = new Set(routes.map((route) => route.role).filter(Boolean));
  for (const item of navigationItems) {
    const surface = surfacesByPath.get(item.path);
    if (!surface) {
      errors.push(`Navigation item ${item.path} is absent from target-surface inventory`);
      continue;
    }
    if (surface.status !== 'implemented') {
      errors.push(`Navigation item ${item.path} points to ${surface.status} surface`);
    }
    if (item.role) {
      exposedRoles.add(item.role);
      if (!surface.roles.includes(item.role)) {
        errors.push(`Navigation role ${item.role} is absent from ${item.path} inventory roles`);
      }
      if (!(surface.actions || []).some((action) => (action.roles || []).includes(item.role))) {
        errors.push(`Navigation item ${item.path} has no useful action bound to role ${item.role}`);
      }
    }
  }
  for (const role of exposedRoles) {
    const hasRoleAction = [...surfacesByPath.values()].some((surface) =>
      surface.status === 'implemented' &&
      (surface.actions || []).some((action) => (action.roles || []).includes(role)));
    if (!hasRoleAction) errors.push(`Role ${role} has no implemented role-bound useful action`);
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
  const routeResult = readRoutes(routeFile);
  const navigationResult = readNavigation(navigationFile);
  const markerFindings = findMarkers(frontendRoot, inventory.forbiddenVisibleMarkers || []);
  const errors = validateModel({
    inventory,
    routes: routeResult.routes,
    navigationItems: navigationResult.items,
    markerFindings,
    parserFindings: [...routeResult.errors, ...navigationResult.errors],
    checkReferences: true
  });

  if (errors.length) {
    errors.forEach((error) => console.error(`FAIL ${error}`));
    console.error(`TARGET SURFACE AUDIT FAILED: ${errors.length} violation(s)`);
    process.exit(1);
  }

  console.log(`TARGET SURFACE AUDIT OK: ${inventory.surfaces.length} surfaces, ${routeResult.routes.length} routes, ${navigationResult.items.length} navigation items`);
}

if (require.main === module) run();

module.exports = {
  findMarkers,
  normalizeRoute,
  readNavigation,
  readRoutes,
  validateModel
};
