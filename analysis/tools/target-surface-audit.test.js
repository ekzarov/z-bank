'use strict';

const assert = require('node:assert/strict');
const { validateModel } = require('./target-surface-audit');

const action = {
  name: 'Perform useful work',
  requirements: ['analysis/tools/target-surface-audit.test.js#Perform useful work'],
  code: ['analysis/tools/target-surface-audit.test.js'],
  tests: ['analysis/tools/target-surface-audit.test.js#target-surface-audit tests']
};

const cleanInventory = {
  surfaces: [
    { id: 'home', path: '/', roles: ['User'], visibility: 'navigation', status: 'implemented', actions: [action] }
  ]
};

assert.deepEqual(validateModel({
  inventory: cleanInventory,
  routePaths: ['/'],
  navigationItems: [{ path: '/', label: 'Home', role: 'User' }],
  markerFindings: [],
  checkReferences: true
}), []);

const errors = validateModel({
  inventory: {
    surfaces: [
      { id: 'admin', path: '/admin', roles: ['Administrator'], visibility: 'navigation', status: 'gap', actions: [] }
    ]
  },
  routePaths: ['/admin'],
  navigationItems: [{ path: '/admin', label: 'Administration', role: 'Administrator' }],
  markerFindings: ['admin.component.ts contains visible placeholder marker "coming soon"']
});

assert(errors.some((error) => error.includes('visible unresolved gap')));
assert(errors.some((error) => error.includes('points to gap surface')));
assert(errors.some((error) => error.includes('Role Administrator has no implemented')));
assert(errors.some((error) => error.includes('placeholder marker')));

console.log('target-surface-audit tests: 2 passed');
