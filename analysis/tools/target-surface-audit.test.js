'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const {
  findMarkers,
  readNavigation,
  readRoutes,
  validateModel
} = require('./target-surface-audit');

const roleBoundTestTags = 'roleBoundTestTags @surface:home @role:User';
if (false) test('roleBoundTestTags @surface:home @role:User', () => {});
const action = {
  name: 'Perform useful work',
  roles: ['User'],
  requirements: ['analysis/tools/target-surface-audit.test.js#Perform useful work'],
  code: ['analysis/tools/target-surface-audit.test.js'],
  tests: [{
    reference: 'analysis/tools/target-surface-audit.test.js#roleBoundTestTags',
    roles: ['User']
  }]
};

const cleanInventory = {
  surfaces: [
    { id: 'home', path: '/', roles: ['User'], visibility: 'navigation', status: 'implemented', actions: [action] }
  ]
};

assert.deepEqual(validateModel({
  inventory: cleanInventory,
  routes: [{ path: '/', role: 'User' }],
  navigationItems: [{ path: '/', label: 'Home', role: 'User' }],
  markerFindings: [],
  checkReferences: true
}), []);

const gapErrors = validateModel({
  inventory: {
    surfaces: [
      { id: 'admin', path: '/admin', roles: ['Administrator'], visibility: 'navigation', status: 'gap', actions: [] }
    ]
  },
  routes: [{ path: '/admin', role: 'Administrator' }],
  navigationItems: [{ path: '/admin', label: 'Administration', role: 'Administrator' }],
  markerFindings: ['admin.component.ts contains visible placeholder marker "coming soon"']
});

assert(gapErrors.some((error) => error.includes('visible unresolved gap')));
assert(gapErrors.some((error) => error.includes('points to gap surface')));
assert(gapErrors.some((error) => error.includes('Role Administrator has no implemented')));
assert(gapErrors.some((error) => error.includes('placeholder marker')));

const hiddenRouteErrors = validateModel({
  inventory: {
    surfaces: [
      { id: 'future', path: '/future', roles: ['User'], visibility: 'hidden', status: 'deferred', actions: [] }
    ]
  },
  routes: [{ path: '/future', role: 'User' }],
  navigationItems: [],
  markerFindings: []
});
assert(hiddenRouteErrors.some((error) => error.includes('remains reachable')));

const roleMismatchErrors = validateModel({
  inventory: cleanInventory,
  routes: [{ path: '/', role: 'Administrator' }],
  navigationItems: [{ path: '/', label: 'Home', role: 'Administrator' }],
  markerFindings: []
});
assert(roleMismatchErrors.some((error) => error.includes('route guard role Administrator')));
assert(roleMismatchErrors.some((error) => error.includes('Navigation role Administrator')));
assert(roleMismatchErrors.some((error) => error.includes('no implemented role-bound useful action')));

const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'target-surface-audit-'));
try {
  const routesFile = path.join(tempRoot, 'routes.ts');
  const navigationFile = path.join(tempRoot, 'navigation.ts');
  const componentFile = path.join(tempRoot, 'future.component.ts');
  fs.writeFileSync(routesFile, `
    export const routes = [
      { component: Parent, path: "parent", children: [
        { data: { title: 'Edit', role: 'Operator' }, path: 'edit', component: Edit }
      ] }
    ];
  `);
  fs.writeFileSync(navigationFile, `
    const items = [
      { icon: 'edit', role: 'Operator', label: 'Edit', path: '/parent/edit' }
    ];
  `);
  fs.writeFileSync(componentFile, `
    @Component({ selector: 'app-future', template: '<p>Future feature</p>' })
    export class FutureComponent {}
  `);

  assert.deepEqual(readRoutes(routesFile), {
    routes: [
      { path: '/parent', role: null },
      { path: '/parent/edit', role: 'Operator' }
    ],
    errors: []
  });
  assert.deepEqual(readNavigation(navigationFile), {
    items: [{ path: '/parent/edit', label: 'Edit', role: 'Operator' }],
    errors: []
  });
  assert(findMarkers(tempRoot, ['future feature']).some((finding) => finding.includes('future.component.ts')));
} finally {
  fs.rmSync(tempRoot, { recursive: true, force: true });
}

assert.equal(roleBoundTestTags, 'roleBoundTestTags @surface:home @role:User');
console.log('target-surface-audit tests: 5 scenarios passed');
