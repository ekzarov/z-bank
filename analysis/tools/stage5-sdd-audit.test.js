'use strict';

const assert = require('node:assert/strict');
const { implementationHasStarted } = require('./stage5-sdd-audit');

const status = (number, lastCompleted) => `methodology_stage:
  number: ${number}
  name: test
  status: test
  last_completed_stage: ${lastCompleted}

next_action:
  actor: test
`;

assert.equal(implementationHasStarted(status(5, 4)), false, 'initial Stage 5 must remain pre-implementation');
assert.equal(implementationHasStarted(status(7, 6)), true, 'Stage 7 starts implementation');
assert.equal(implementationHasStarted(status(5, 9)), true, 'Stage 10 correction loop retains implementation history');

console.log('stage5-sdd-audit lifecycle tests: 3 passed');
