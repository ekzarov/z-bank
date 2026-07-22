const test = require('node:test');
const assert = require('node:assert/strict');
const { LegacyBankSimulator, LegacySimulationError } = require('../src/bank-simulator');

test('CICS menu dispatches known choices and redisplays invalid choices', () => {
    const simulator = new LegacyBankSimulator();
    assert.equal(simulator.cicsMenu('1').action, 'DISPLAY_CUSTOMER');
    assert.equal(simulator.cicsMenu('5').action, 'UPDATE_ACCOUNT');
    assert.equal(simulator.cicsMenu('6').action, 'CASH_TRANSACTION');
    assert.equal(simulator.cicsMenu('7').action, 'TRANSFER');
    assert.equal(simulator.cicsMenu('A').action, 'CUSTOMER_ACCOUNTS');
    assert.deepEqual(simulator.cicsMenu('9'), {
        action: 'REDISPLAY',
        message: 'You must enter a valid value (1-7 or A).'
    });
    assert.deepEqual(simulator.cicsMenu('PF7'), {
        action: 'REDISPLAY',
        message: 'Invalid key pressed.'
    });
    assert.deepEqual(simulator.cicsMenu('PF12'), { action: 'EXIT', message: 'SESSION ENDED' });
    assert.deepEqual(simulator.cicsMenu('PA1'), { action: 'REDISPLAY', message: '' });
});

test('CICS payment rejects insufficient funds while teller permits the debit', () => {
    const simulator = new LegacyBankSimulator();
    assert.throws(
        () => simulator.cicsCash('10000001', -5000, { facilityType: 496 }),
        error => error instanceof LegacySimulationError && error.code === 'INSUFFICIENT_FUNDS'
    );
    const tellerResult = simulator.cicsCash('10000001', -5000, { facilityType: 0 });
    assert.equal(tellerResult.availableBalance, -3750);
});

test('CICS payment rejects cash activity for loan accounts', () => {
    const simulator = new LegacyBankSimulator();
    assert.throws(
        () => simulator.cicsCash('10000002', 100, { facilityType: 496 }),
        error => error.code === 'PRODUCT_RESTRICTED'
    );
});

test('CICS transfer mutates both accounts and writes one source history record', () => {
    const simulator = new LegacyBankSimulator();
    const countBefore = simulator.state.transactions.length;
    simulator.transfer('10000001', '10000002', 25);
    assert.equal(simulator.findAccount('CICS', '10000001').actualBalance, 1225);
    assert.equal(simulator.findAccount('CICS', '10000002').actualBalance, -4975);
    assert.equal(simulator.state.transactions.length, countBefore + 1);
    assert.match(simulator.state.transactions.at(-1).transactionInformation, /10000002/);
});

test('CICS transfer preserves the observed absence of a pre-transfer funds rejection', () => {
    const simulator = new LegacyBankSimulator();
    simulator.transfer('10000001', '10000002', 5000);
    assert.equal(simulator.findAccount('CICS', '10000001').actualBalance, -3750);
});

test('money operations reject values with more than two decimal places', () => {
    const simulator = new LegacyBankSimulator();
    for (const operation of [
        () => simulator.cicsCash('10000001', 0.001),
        () => simulator.depositCics('10000001', { amount: 0.001, sortCode: '987654' }),
        () => simulator.depositIms('000000001', '101', { amount: 0.001, sortCode: '987654' }),
        () => simulator.transfer('10000001', '10000002', 0.001)
    ]) {
        assert.throws(operation, error => ['INVALID_AMOUNT', 'INVALID_DEPOSIT'].includes(error.code));
    }
});

test('CICS account list omits customerId so the legacy page does not misroute balances to IMS', () => {
    const simulator = new LegacyBankSimulator();
    const cicsAccount = simulator.listAccounts('CICS', '0000000001').accounts[0];
    const imsAccount = simulator.listAccounts('IMS', '000000001').accounts[0];
    assert.equal(cicsAccount.customerId, undefined);
    assert.equal(imsAccount.customerId, '000000001');
});

test('IMS session rejects duplicate login and preserves observed logout false success', () => {
    const simulator = new LegacyBankSimulator();
    simulator.loginIms('000000001', 'password');
    assert.throws(
        () => simulator.loginIms('000000001', 'password'),
        error => error.code === 'ALREADY_LOGGED_IN'
    );
    const result = simulator.logoutIms('000000001', true);
    assert.equal(result.message, 'LOGOFF SUCCESSFUL');
    assert.equal(result.replacementApplied, false);
    assert.equal(simulator.findCustomer('IMS', '000000001').loggedIn, true);
});

test('IMS session distinguishes invalid credentials and missing customers', () => {
    const simulator = new LegacyBankSimulator();
    assert.throws(
        () => simulator.loginIms('000000001', 'wrong'),
        error => error.code === 'INVALID_PASSWORD' && error.message === 'PASSWORD INVALID'
    );
    assert.throws(
        () => simulator.loginIms('999999999', 'password'),
        error => error.code === 'CUSTOMER_DOES_NOT_EXIST' && error.message === 'CUSTOMER DOES NOT EXIST'
    );
    assert.throws(
        () => simulator.logoutIms('999999999'),
        error => error.code === 'FAILED_LOGOFF_UPDATE' && error.message === 'FAILED UPDATE FOR LOGOFF'
    );
});

test('IMS login records the observed login timestamp', () => {
    const simulator = new LegacyBankSimulator();
    simulator.loginIms('000000001', 'password');
    assert.equal(simulator.findCustomer('IMS', '000000001').loginTimestamp, '2026_07_21 12:00:00:000');
});

test('IMS zero transaction advances history without changing balance', () => {
    const simulator = new LegacyBankSimulator();
    const account = simulator.findAccount('IMS', '101');
    const balanceBefore = account.actualBalance;
    const transactionCount = simulator.state.transactions.length;
    simulator.imsTransaction('000000001', '101', 'd', 0);
    assert.equal(account.actualBalance, balanceBefore);
    assert.equal(account.lastTransactionId, 2);
    assert.equal(simulator.state.transactions.length, transactionCount + 1);
});

test('IMS signed amount can reverse the requested transaction direction', () => {
    const simulator = new LegacyBankSimulator();
    const before = simulator.findAccount('IMS', '101').actualBalance;
    simulator.imsTransaction('000000001', '101', 'w', -10);
    assert.equal(simulator.findAccount('IMS', '101').actualBalance, before + 10);
});

test('IMS direct message preserves the account ownership gap', () => {
    const simulator = new LegacyBankSimulator();
    const result = simulator.imsTransaction('000000001', '201', 'w', 50);
    assert.equal(result.mutatedAccountCustomerId, '000000002');
    assert.equal(result.responsePortfolioCustomerId, '000000001');
    assert.deepEqual(result.availableBalance, [8830, 6325]);
    assert.equal(simulator.findAccount('IMS', '201').actualBalance, 7895);
});

test('IMS direct message distinguishes invalid action and missing account', () => {
    const simulator = new LegacyBankSimulator();
    assert.throws(
        () => simulator.imsTransaction('000000001', '101', 'x', 10),
        error => error.code === 'INVALID_ACTION'
    );
    assert.throws(
        () => simulator.imsTransaction('000000001', '999', 'd', 10),
        error => error.code === 'ACCOUNT_NOT_FOUND'
    );
});

test('IMS missing response customer is reported after account mutation', () => {
    const simulator = new LegacyBankSimulator();
    const account = simulator.findAccount('IMS', '101');
    const balanceBefore = account.actualBalance;
    const transactionCount = simulator.state.transactions.length;
    assert.throws(
        () => simulator.imsTransaction('999999999', '101', 'd', 10),
        error => error.code === 'CUSTOMER_DOES_NOT_EXIST' && error.message === 'CUSTOMER DOES NOT EXIST'
    );
    assert.equal(account.actualBalance, balanceBefore + 10);
    assert.equal(simulator.state.transactions.length, transactionCount + 1);
});

test('OpenBanking deposit validation blocks direct IMS signed amount quirks', () => {
    const simulator = new LegacyBankSimulator();
    assert.throws(
        () => simulator.depositIms('000000001', '101', { amount: -10, sortCode: '987654' }),
        error => error.code === 'INVALID_DEPOSIT'
    );
});

test('monthly statement reports period transactions and reconciles the opening balance', () => {
    const simulator = new LegacyBankSimulator();
    const statement = simulator.monthlyStatement({ accountId: '10000001', reportingMonth: '202607' });
    assert.equal(statement.periodFrom, '20260701');
    assert.equal(statement.periodTo, '20260731');
    assert.equal(statement.summary.transactionCount, 2);
    assert.equal(statement.summary.totalCredits, 250);
    assert.equal(statement.summary.totalDebits, 40);
    assert.equal(statement.summary.openingBalance, 1040);
    assert.equal(statement.summary.closingBalance, 1250);
    assert.equal(statement.summary.availableBalance, 1250);
    assert.equal(statement.customer.customerId, '0000000001');
    assert.equal(statement.account.accountId, '10000001');
    assert.equal(statement.transactions[0].currency, statement.account.currency);
    assert.ok(statement.transactions.every(transaction => transaction.transactionId
        && transaction.bookingDateTime && transaction.creditDebitIndicator));
    assert.equal(statement.footer, 'END OF STATEMENT');
});

test('monthly statement emits the legacy empty-history message', () => {
    const simulator = new LegacyBankSimulator();
    const statement = simulator.monthlyStatement({ accountId: '10000002', reportingMonth: '202606' });
    assert.equal(statement.emptyHistoryMessage, 'NO TRANSACTIONS FOR THIS PERIOD');
    assert.equal(statement.summary.transactionCount, 0);
});
