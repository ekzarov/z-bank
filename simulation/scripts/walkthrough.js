const { LegacyBankSimulator } = require('../src/bank-simulator');

const simulator = new LegacyBankSimulator();

function step(name, run) {
    try {
        console.log(`\n=== ${name} ===`);
        console.log(JSON.stringify(run(), null, 2));
    } catch (error) {
        console.log(JSON.stringify({ code: error.code, message: error.message }, null, 2));
    }
}

console.log('Bank of Z Stage 3 traceable simulated walkthrough');
console.log('SIMULATION ONLY: results are derived from source/contracts, not an IBM runtime.');

step('CICS menu dispatch', () => simulator.cicsMenu('5'));
step('CICS invalid menu choice', () => simulator.cicsMenu('9'));
step('CICS teller debit', () => simulator.cicsCash('10000001', -100, { facilityType: 0, description: 'Teller cash' }));
step('CICS payment insufficient funds', () => simulator.cicsCash('10000001', -5000, { facilityType: 496 }));
step('CICS atomic transfer', () => simulator.transfer('10000001', '10000002', 25));
step('IMS login', () => simulator.loginIms('000000001', 'password'));
step('IMS direct zero-value transaction', () => simulator.imsTransaction('000000001', '101', 'd', 0));
step('IMS ownership gap', () => simulator.imsTransaction('000000001', '201', 'w', 50));
step('OpenBanking CICS customer', () => simulator.getCustomer('CICS', '0000000001'));
step('Monthly statement', () => simulator.monthlyStatement({ accountId: '10000001', reportingMonth: '202607' }));

console.log('\nEvidence index:');
console.log(JSON.stringify(simulator.state.evidence, null, 2));
