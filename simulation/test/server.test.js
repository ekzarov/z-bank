const test = require('node:test');
const assert = require('node:assert/strict');
const { createSimulationServer } = require('../src/server');

async function withServer(run) {
    const server = createSimulationServer();
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const address = server.address();
    try {
        await run(`http://127.0.0.1:${address.port}`);
    } finally {
        await new Promise((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
    }
}

test('health identifies the contour as simulated', async () => {
    await withServer(async baseUrl => {
        const response = await fetch(`${baseUrl}/health`);
        const payload = await response.json();
        assert.equal(response.status, 200);
        assert.equal(response.headers.get('x-simulation-only'), 'true');
        assert.equal(payload.evidenceClass, 'simulated');
    });
});

test('CICS web API lookup and deposit use contract-shaped responses', async () => {
    await withServer(async baseUrl => {
        const customerResponse = await fetch(`${baseUrl}/api/customers/0000000001`);
        const customer = await customerResponse.json();
        assert.equal(customer.firstName, 'Martha');
        assert.match(customerResponse.headers.get('x-legacy-evidence'), /openapi\.yaml/);

        const depositResponse = await fetch(`${baseUrl}/api/accounts/10000001/deposit`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ amount: 50, sortCode: '987654', description: 'Web deposit' })
        });
        const deposit = await depositResponse.json();
        assert.equal(depositResponse.status, 201);
        assert.equal(deposit.availableBalance, 1300);
        assert.equal(Array.isArray(deposit.availableBalance), false);
    });
});

test('customer API creates, updates, and rejects unknown customers', async () => {
    await withServer(async baseUrl => {
        const create = await fetch(`${baseUrl}/api/customers`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                title: 'Mr', firstName: 'Sam', lastName: 'Taylor',
                address: { addressLine1: '1 Main Street', city: 'London', postalCode: 'E1 1AA', country: 'UK' }
            })
        });
        const created = await create.json();
        assert.equal(create.status, 201);

        const update = await fetch(`${baseUrl}/api/customers/${created.customerId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ phoneNumber: '555-0199' })
        });
        assert.equal((await update.json()).phoneNumber, '555-0199');

        const missing = await fetch(`${baseUrl}/api/customers/9999999999`);
        assert.equal(missing.status, 404);
        assert.equal((await missing.json()).code, 'CUSTOMER_NOT_FOUND');
    });
});

test('IMS web API deposit returns portfolio arrays used by the legacy page', async () => {
    await withServer(async baseUrl => {
        const response = await fetch(`${baseUrl}/api/ims/accounts/000000001/101/deposit`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ amount: 25, sortCode: '987654' })
        });
        const payload = await response.json();
        assert.equal(response.status, 201);
        assert.deepEqual(payload.availableBalance, [8855, 6325]);
    });
});

test('published transaction and account-list routes remain explicitly unbound', async () => {
    await withServer(async baseUrl => {
        const accounts = await fetch(`${baseUrl}/api/accounts`);
        const accountPayload = await accounts.json();
        assert.equal(accounts.status, 501);
        assert.equal(accountPayload.code, 'LEGACY_MAPPING_MISSING');

        const transactions = await fetch(`${baseUrl}/api/accounts/10000001/transactions`);
        const transactionPayload = await transactions.json();
        assert.equal(transactions.status, 501);
        assert.equal(transactionPayload.code, 'LEGACY_MAPPING_MISSING');
    });
});

test('simulation-only endpoints expose terminal and batch behavior with evidence', async () => {
    await withServer(async baseUrl => {
        const menu = await fetch(`${baseUrl}/simulation/cics/menu`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ choice: '6' })
        });
        assert.equal((await menu.json()).action, 'TRANSFER');
        assert.match(menu.headers.get('x-legacy-evidence'), /BNKMENU\.cbl/);

        const statement = await fetch(`${baseUrl}/simulation/batch/statements/10000001?month=202607`);
        const payload = await statement.json();
        assert.equal(payload.summary.transactionCount, 2);
        assert.match(statement.headers.get('x-legacy-evidence'), /BNKSTMT\.pli/);
    });
});
