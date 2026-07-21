const http = require('node:http');
const { LegacyBankSimulator, LegacySimulationError } = require('./bank-simulator');

function json(response, status, body, evidence) {
    const headers = {
        'Content-Type': 'application/json; charset=utf-8',
        'X-Simulation-Only': 'true'
    };
    if (evidence) headers['X-Legacy-Evidence'] = evidence.sources.join('; ');
    response.writeHead(status, headers);
    response.end(JSON.stringify(body));
}

async function body(request) {
    let value = '';
    for await (const chunk of request) value += chunk;
    if (!value) return {};
    try {
        return JSON.parse(value);
    } catch {
        throw new LegacySimulationError(400, 'INVALID_JSON', 'Request body must be valid JSON');
    }
}

function route(method, pathname, pattern) {
    if (method !== pattern.method) return null;
    const match = pathname.match(pattern.path);
    return match ? match.groups || {} : null;
}

function createSimulationServer(simulator = new LegacyBankSimulator()) {
    return http.createServer(async (request, response) => {
        const url = new URL(request.url, 'http://simulation.local');
        const method = request.method.toUpperCase();

        try {
            if (method === 'GET' && url.pathname === '/health') {
                return json(response, 200, {
                    status: 'ok',
                    evidenceClass: 'simulated',
                    warning: simulator.state.metadata.warning
                });
            }

            if (method === 'GET' && url.pathname === '/simulation/evidence') {
                return json(response, 200, simulator.state.evidence);
            }

            if (method === 'POST' && url.pathname === '/api/customers') {
                return json(response, 201, simulator.createCicsCustomer(await body(request)), simulator.evidence('api-contract'));
            }

            let params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/customers\/(?<customerId>[^/]+)$/
            });
            if (params) return json(response, 200, simulator.getCustomer('CICS', params.customerId), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'PUT', path: /^\/api\/customers\/(?<customerId>[^/]+)$/
            });
            if (params) return json(response, 200, simulator.updateCustomer('CICS', params.customerId, await body(request)), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/customers\/(?<customerId>[^/]+)\/accounts$/
            });
            if (params) return json(response, 200, simulator.listAccounts('CICS', params.customerId), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/accounts\/(?<accountId>[^/]+)$/
            });
            if (params) return json(response, 200, simulator.getAccount('CICS', params.accountId), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/accounts\/(?<accountId>[^/]+)\/balances$/
            });
            if (params) return json(response, 200, simulator.getBalance('CICS', params.accountId), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'POST', path: /^\/api\/accounts\/(?<accountId>[^/]+)\/deposit$/
            });
            if (params) return json(response, 201, simulator.depositCics(params.accountId, await body(request)), simulator.evidence('cics-cash'));

            if (method === 'GET' && url.pathname === '/api/accounts') {
                throw new LegacySimulationError(501, 'LEGACY_MAPPING_MISSING', 'GET /accounts is published but has no legacy operation mapping');
            }
            if (method === 'GET' && /^\/api\/accounts\/[^/]+\/transactions(?:\/[^/]+)?$/.test(url.pathname)) {
                throw new LegacySimulationError(501, 'LEGACY_MAPPING_MISSING', 'The transaction REST route is published but has no legacy operation mapping');
            }

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/ims\/customers\/(?<customerId>[^/]+)$/
            });
            if (params) return json(response, 200, simulator.getCustomer('IMS', params.customerId), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'PUT', path: /^\/api\/ims\/customers\/(?<customerId>[^/]+)$/
            });
            if (params) return json(response, 200, simulator.updateCustomer('IMS', params.customerId, await body(request)), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/ims\/customers\/(?<customerId>[^/]+)\/accounts$/
            });
            if (params) return json(response, 200, simulator.listAccounts('IMS', params.customerId), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/ims\/accounts\/(?<customerId>[^/]+)$/
            });
            if (params) {
                const accounts = simulator.listAccounts('IMS', params.customerId);
                return json(response, 200, accounts, simulator.evidence('api-contract'));
            }

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/api\/ims\/accounts\/(?<customerId>[^/]+)\/balances$/
            });
            if (params) return json(response, 200, simulator.getBalance('IMS', params.customerId), simulator.evidence('api-contract'));

            params = route(method, url.pathname, {
                method: 'POST', path: /^\/api\/ims\/accounts\/(?<customerId>[^/]+)\/(?<accountId>[^/]+)\/deposit$/
            });
            if (params) return json(response, 201, simulator.depositIms(params.customerId, params.accountId, await body(request)), simulator.evidence('ims-transaction'));

            if (method === 'POST' && url.pathname === '/simulation/cics/menu') {
                const input = await body(request);
                return json(response, 200, simulator.cicsMenu(input.choice), simulator.evidence('cics-menu'));
            }
            if (method === 'POST' && url.pathname === '/simulation/cics/cash') {
                const input = await body(request);
                return json(response, 200, simulator.cicsCash(input.accountId, input.amount, input), simulator.evidence('cics-cash'));
            }
            if (method === 'POST' && url.pathname === '/simulation/cics/transfer') {
                const input = await body(request);
                return json(response, 200, simulator.transfer(input.fromAccountId, input.toAccountId, input.amount, input.description), simulator.evidence('cics-transfer'));
            }
            if (method === 'POST' && url.pathname === '/simulation/ims/login') {
                const input = await body(request);
                return json(response, 200, simulator.loginIms(input.customerId, input.password), simulator.evidence('ims-session'));
            }
            if (method === 'POST' && url.pathname === '/simulation/ims/logout') {
                const input = await body(request);
                return json(response, 200, simulator.logoutIms(input.customerId, input.simulateReplacementFailure), simulator.evidence('ims-session'));
            }
            if (method === 'POST' && url.pathname === '/simulation/ims/transaction') {
                const input = await body(request);
                return json(response, 200,
                    simulator.imsTransaction(input.customerId, input.accountId, input.action, input.amount, input.description),
                    simulator.evidence('ims-transaction'));
            }

            params = route(method, url.pathname, {
                method: 'GET', path: /^\/simulation\/batch\/statements\/(?<accountId>[^/]+)$/
            });
            if (params) {
                return json(response, 200, simulator.monthlyStatement({
                    accountId: params.accountId,
                    reportingMonth: url.searchParams.get('month') || '202607',
                    sortCode: url.searchParams.get('sortCode') || '123456'
                }), simulator.evidence('monthly-statement'));
            }

            json(response, 404, { code: 'NOT_FOUND', message: 'Simulation route not found' });
        } catch (error) {
            if (error instanceof LegacySimulationError) {
                return json(response, error.status, { code: error.code, message: error.message, details: error.details });
            }
            console.error(error);
            json(response, 500, { code: 'SIMULATION_ERROR', message: error.message });
        }
    });
}

if (require.main === module) {
    const port = Number(process.env.PORT || 9080);
    createSimulationServer().listen(port, '0.0.0.0', () => {
        console.log(`Bank of Z legacy simulator listening on http://localhost:${port}`);
        console.log('SIMULATION ONLY: this does not verify IBM CICS, IMS, or DB2 runtime behavior.');
    });
}

module.exports = { createSimulationServer };
