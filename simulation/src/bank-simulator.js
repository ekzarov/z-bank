const fixture = require('../fixtures/legacy-fixture.json');

class LegacySimulationError extends Error {
    constructor(status, code, message, details = []) {
        super(message);
        this.status = status;
        this.code = code;
        this.details = details;
    }
}

function clone(value) {
    return JSON.parse(JSON.stringify(value));
}

function money(value) {
    return Math.round(Number(value) * 100) / 100;
}

function hasMoneyPrecision(value) {
    const number = Number(value);
    return Number.isFinite(number)
        && Math.abs(number * 100 - Math.round(number * 100)) < 1e-9;
}

function imsTimestamp(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return String(value);
    const pad = (part, size = 2) => String(part).padStart(size, '0');
    return `${date.getUTCFullYear()}_${pad(date.getUTCMonth() + 1)}_${pad(date.getUTCDate())} `
        + `${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())}:${pad(date.getUTCSeconds())}`
        + `:${pad(date.getUTCMilliseconds(), 3)}`;
}

class LegacyBankSimulator {
    constructor(seed = fixture) {
        this.seed = clone(seed);
        this.reset();
    }

    reset() {
        this.state = clone(this.seed);
        this.sequence = this.state.transactions.length + 1;
    }

    evidence(key) {
        return clone(this.state.evidence[key]);
    }

    findCustomer(system, customerId) {
        const customer = this.state.customers.find(item =>
            item.system === system && item.customerId === String(customerId));
        if (!customer) {
            throw new LegacySimulationError(404, 'CUSTOMER_NOT_FOUND', 'Customer not found');
        }
        return customer;
    }

    getCustomer(system, customerId) {
        const customer = clone(this.findCustomer(system, customerId));
        delete customer.system;
        delete customer.password;
        delete customer.loggedIn;
        delete customer.creditScore;
        return customer;
    }

    createCicsCustomer(input) {
        const required = ['firstName', 'lastName'];
        const missing = required.filter(field => !String(input[field] || '').trim());
        if (missing.length || !input.address?.addressLine1) {
            throw new LegacySimulationError(400, 'INVALID_CUSTOMER', 'Mandatory customer data is missing',
                [...missing, ...(!input.address?.addressLine1 ? ['address.addressLine1'] : [])]
                    .map(field => ({ field, issue: 'Required' })));
        }
        const next = Math.max(...this.state.customers
            .filter(item => item.system === 'CICS')
            .map(item => Number(item.customerId))) + 1;
        const customerId = String(next).padStart(10, '0');
        this.state.customers.push({
            system: 'CICS',
            customerId,
            title: input.title || '',
            firstName: input.firstName,
            lastName: input.lastName,
            dateOfBirth: input.dateOfBirth || '',
            phoneNumber: input.phoneNumber || '',
            address: clone(input.address),
            customerStatus: input.customerStatus || 'ACTIVE',
            createdDate: this.state.metadata.clock,
            creditScore: input.simulateNoCreditAgencyResponse ? 0 : 650
        });
        return { customerId, sortCode: this.state.metadata.sortCode };
    }

    updateCustomer(system, customerId, updates) {
        const customer = this.findCustomer(system, customerId);
        const simpleFields = ['title', 'firstName', 'lastName', 'dateOfBirth', 'phoneNumber', 'customerStatus'];
        for (const field of simpleFields) {
            if (updates[field] !== undefined) customer[field] = updates[field];
        }
        if (updates.address) customer.address = { ...customer.address, ...clone(updates.address) };
        return this.getCustomer(system, customerId);
    }

    loginIms(customerId, password) {
        let customer;
        try {
            customer = this.findCustomer('IMS', customerId);
        } catch (error) {
            if (error.code === 'CUSTOMER_NOT_FOUND') {
                throw new LegacySimulationError(404, 'CUSTOMER_DOES_NOT_EXIST', 'CUSTOMER DOES NOT EXIST');
            }
            throw error;
        }
        if (customer.password !== password) {
            throw new LegacySimulationError(401, 'INVALID_PASSWORD', 'PASSWORD INVALID');
        }
        if (customer.loggedIn) {
            throw new LegacySimulationError(409, 'ALREADY_LOGGED_IN', 'CUSTOMER ALREADY LOGGED IN');
        }
        customer.loggedIn = true;
        customer.loginTimestamp = imsTimestamp(this.state.metadata.clock);
        return { message: 'LOGIN SUCCESSFUL', customerId };
    }

    logoutIms(customerId, simulateReplacementFailure = false) {
        let customer;
        try {
            customer = this.findCustomer('IMS', customerId);
        } catch (error) {
            if (error.code === 'CUSTOMER_NOT_FOUND') {
                throw new LegacySimulationError(404, 'FAILED_LOGOFF_UPDATE', 'FAILED UPDATE FOR LOGOFF');
            }
            throw error;
        }
        if (!simulateReplacementFailure) customer.loggedIn = false;
        return {
            message: 'LOGOFF SUCCESSFUL',
            replacementApplied: !simulateReplacementFailure,
            observedFalseSuccess: simulateReplacementFailure
        };
    }

    listAccounts(system, customerId) {
        this.findCustomer(system, customerId);
        const accounts = this.state.accounts
            .filter(item => item.system === system && item.customerId === String(customerId))
            .map(item => this.publicAccount(item));
        return { accounts, totalCount: accounts.length };
    }

    findAccount(system, accountId) {
        const account = this.state.accounts.find(item =>
            item.system === system && item.accountId === String(accountId));
        if (!account) {
            throw new LegacySimulationError(404, 'ACCOUNT_NOT_FOUND', 'Account not found');
        }
        return account;
    }

    publicAccount(account) {
        const result = {
            accountId: account.accountId,
            accountType: account.accountType,
            currency: account.currency,
            accountNumber: account.accountNumber,
            sortCode: this.state.metadata.sortCode,
            status: account.status,
            openingDate: account.openingDate
        };
        if (account.system === 'IMS') result.customerId = account.customerId;
        return result;
    }

    getAccount(system, accountId) {
        return this.publicAccount(this.findAccount(system, accountId));
    }

    getBalance(system, accountOrCustomerId) {
        let accounts;
        if (system === 'IMS') {
            accounts = this.state.accounts.filter(item =>
                item.system === 'IMS' && item.customerId === String(accountOrCustomerId));
            if (!accounts.length) {
                throw new LegacySimulationError(404, 'ACCOUNT_NOT_FOUND', 'Account not found');
            }
            return {
                balanceType: 'AVAILABLE',
                amount: accounts.map(item => item.availableBalance),
                currency: 'USD',
                creditDebitIndicator: 'CREDIT',
                dateTime: this.state.metadata.clock
            };
        }
        const account = this.findAccount('CICS', accountOrCustomerId);
        return {
            balanceType: 'AVAILABLE',
            amount: account.availableBalance,
            currency: account.currency,
            creditDebitIndicator: account.availableBalance < 0 ? 'DEBIT' : 'CREDIT',
            dateTime: this.state.metadata.clock
        };
    }

    recordTransaction(system, account, signedAmount, description) {
        const transaction = {
            system,
            transactionId: `${system === 'IMS' ? account.accountId : 'PRTR'}-${String(this.sequence++).padStart(6, '0')}`,
            accountId: account.accountId,
            amount: Math.abs(money(signedAmount)),
            currency: account.currency,
            creditDebitIndicator: signedAmount < 0 ? 'DEBIT' : 'CREDIT',
            status: 'BOOKED',
            bookingDateTime: this.state.metadata.clock,
            transactionInformation: description || ''
        };
        this.state.transactions.push(transaction);
        return transaction;
    }

    cicsCash(accountId, signedAmount, { facilityType = 0, description = '' } = {}) {
        const amount = Number(signedAmount);
        if (!hasMoneyPrecision(signedAmount) || amount === 0) {
            throw new LegacySimulationError(400, 'INVALID_AMOUNT', 'Amount must be non-zero with at most two decimals');
        }
        const account = this.findAccount('CICS', accountId);
        if (facilityType === 496 && ['LOAN', 'MORTGAGE'].includes(account.accountType)) {
            throw new LegacySimulationError(409, 'PRODUCT_RESTRICTED', 'Payment cash activity is not allowed for this product');
        }
        if (facilityType === 496 && amount < 0 && money(account.availableBalance + amount) < 0) {
            throw new LegacySimulationError(409, 'INSUFFICIENT_FUNDS', 'Insufficient available funds');
        }
        account.availableBalance = money(account.availableBalance + amount);
        account.actualBalance = money(account.actualBalance + amount);
        const transaction = this.recordTransaction('CICS', account, amount, description);
        return {
            accountId: account.accountId,
            availableBalance: account.availableBalance,
            actualBalance: account.actualBalance,
            transactionReference: transaction.transactionId
        };
    }

    depositCics(accountId, request) {
        if (!(Number(request.amount) > 0) || !hasMoneyPrecision(request.amount)
            || !/^\d{6}$/.test(String(request.sortCode || ''))) {
            throw new LegacySimulationError(400, 'INVALID_DEPOSIT', 'Amount must be positive with at most two decimals and sort code must contain six digits');
        }
        return this.cicsCash(accountId, Number(request.amount), {
            facilityType: 0,
            description: request.description || 'Deposit via OpenBanking API'
        });
    }

    imsTransaction(customerId, accountId, action, rawAmount, description = '') {
        const normalizedAction = String(action).toLowerCase();
        if (!['d', 'w'].includes(normalizedAction)) {
            throw new LegacySimulationError(400, 'INVALID_ACTION', "INVALID ACCOUNT ACTION. MUST BE 'w' OR 'd'.");
        }
        const account = this.findAccount('IMS', accountId);
        const amount = Number(rawAmount);
        if (!hasMoneyPrecision(rawAmount)) {
            throw new LegacySimulationError(400, 'INVALID_AMOUNT', 'Amount must be numeric with at most two decimals');
        }
        const signedAmount = normalizedAction === 'w' ? -amount : amount;
        account.availableBalance = money(account.availableBalance + signedAmount);
        account.actualBalance = money(account.actualBalance + signedAmount);
        account.lastTransactionId += 1;
        const transaction = this.recordTransaction('IMS', account, signedAmount, description || `IMS ${normalizedAction}`);
        try {
            this.findCustomer('IMS', customerId);
        } catch (error) {
            if (error.code === 'CUSTOMER_NOT_FOUND') {
                throw new LegacySimulationError(404, 'CUSTOMER_DOES_NOT_EXIST', 'CUSTOMER DOES NOT EXIST');
            }
            throw error;
        }
        const portfolio = this.state.accounts.filter(item =>
            item.system === 'IMS' && item.customerId === String(customerId));
        return {
            accountId: portfolio.map(item => item.accountId).join(','),
            availableBalance: portfolio.map(item => item.availableBalance),
            actualBalance: portfolio.map(item => item.actualBalance),
            transactionReference: transaction.transactionId,
            mutatedAccountCustomerId: account.customerId,
            responsePortfolioCustomerId: String(customerId)
        };
    }

    depositIms(customerId, accountId, request) {
        if (!(Number(request.amount) > 0) || !hasMoneyPrecision(request.amount)
            || !/^\d{6}$/.test(String(request.sortCode || ''))) {
            throw new LegacySimulationError(400, 'INVALID_DEPOSIT', 'Amount must be positive with at most two decimals and sort code must contain six digits');
        }
        return this.imsTransaction(customerId, accountId, 'd', Number(request.amount),
            request.description || 'Deposit via OpenBanking API');
    }

    transfer(fromAccountId, toAccountId, amount, description = 'CICS transfer') {
        if (!(Number(amount) > 0) || !hasMoneyPrecision(amount)) {
            throw new LegacySimulationError(400, 'INVALID_AMOUNT', 'Transfer amount must be positive with at most two decimals');
        }
        if (String(fromAccountId) === String(toAccountId)) {
            throw new LegacySimulationError(409, 'SAME_ACCOUNT', 'Cannot transfer to the same account');
        }
        const from = this.findAccount('CICS', fromAccountId);
        const to = this.findAccount('CICS', toAccountId);
        const before = { from: clone(from), to: clone(to), transactionCount: this.state.transactions.length };
        try {
            from.availableBalance = money(from.availableBalance - Number(amount));
            from.actualBalance = money(from.actualBalance - Number(amount));
            to.availableBalance = money(to.availableBalance + Number(amount));
            to.actualBalance = money(to.actualBalance + Number(amount));
            const transfer = this.recordTransaction(
                'CICS',
                from,
                -Number(amount),
                `${description}; destination ${to.sortCode || this.state.metadata.sortCode}/${to.accountId}`
            );
            return { transactionReference: transfer.transactionId };
        } catch (error) {
            Object.assign(from, before.from);
            Object.assign(to, before.to);
            this.state.transactions.length = before.transactionCount;
            throw error;
        }
    }

    transactions(accountId, { limit = 50, offset = 0 } = {}) {
        this.findAccount('CICS', accountId);
        const rows = this.state.transactions
            .filter(item => item.accountId === String(accountId))
            .sort((left, right) => right.bookingDateTime.localeCompare(left.bookingDateTime));
        return { transactions: rows.slice(offset, offset + limit).map(clone), totalCount: rows.length, limit, offset };
    }

    transaction(accountId, transactionId) {
        const row = this.state.transactions.find(item =>
            item.accountId === String(accountId) && item.transactionId === String(transactionId));
        if (!row) throw new LegacySimulationError(404, 'TRANSACTION_NOT_FOUND', 'Transaction not found');
        return clone(row);
    }

    cicsMenu(input) {
        const normalized = String(input || '').toUpperCase();
        const choices = {
            '1': 'DISPLAY_CUSTOMER', '2': 'DISPLAY_ACCOUNT', '3': 'CREATE_CUSTOMER',
            '4': 'CREATE_ACCOUNT', '5': 'UPDATE_ACCOUNT', '6': 'CASH_TRANSACTION',
            '7': 'TRANSFER', 'A': 'CUSTOMER_ACCOUNTS'
        };
        if (normalized === 'PF3') return { action: 'EXIT', message: 'SESSION ENDED' };
        if (normalized === 'PF12') return { action: 'EXIT', message: 'SESSION ENDED' };
        if (/^PA[123]$/.test(normalized)) return { action: 'REDISPLAY', message: '' };
        if (/^(PF|PA)/.test(normalized)) {
            return { action: 'REDISPLAY', message: 'Invalid key pressed.' };
        }
        if (!choices[normalized]) {
            return { action: 'REDISPLAY', message: 'You must enter a valid value (1-7 or A).' };
        }
        return { action: choices[normalized], message: 'TRANSACTION DISPATCHED' };
    }

    monthlyStatement({ accountId, reportingMonth = '202607', sortCode }) {
        const account = this.findAccount('CICS', accountId);
        const requestedSortCode = String(sortCode || this.state.metadata.sortCode);
        if (requestedSortCode !== String(this.state.metadata.sortCode)) {
            throw new LegacySimulationError(404, 'ACCOUNT_NOT_FOUND', 'Account not found for sort code');
        }
        const customer = this.findCustomer('CICS', account.customerId);
        if (!/^\d{6}$/.test(reportingMonth)) {
            throw new LegacySimulationError(400, 'INVALID_REPORTING_MONTH', 'Reporting month must use YYYYMM');
        }
        const year = reportingMonth.slice(0, 4);
        const month = reportingMonth.slice(4, 6);
        const monthNumber = Number(month);
        if (monthNumber < 1 || monthNumber > 12) {
            throw new LegacySimulationError(400, 'INVALID_REPORTING_MONTH', 'Reporting month must use YYYYMM');
        }
        const days = new Date(Date.UTC(Number(year), monthNumber, 0)).getUTCDate();
        const prefix = `${year}-${month}`;
        const rows = this.state.transactions
            .filter(item => item.accountId === account.accountId && item.bookingDateTime.startsWith(prefix))
            .sort((left, right) => left.bookingDateTime.localeCompare(right.bookingDateTime)
                || left.transactionId.localeCompare(right.transactionId));
        const credits = money(rows.filter(item => item.creditDebitIndicator === 'CREDIT')
            .reduce((total, item) => total + item.amount, 0));
        const debits = money(rows.filter(item => item.creditDebitIndicator === 'DEBIT')
            .reduce((total, item) => total + item.amount, 0));
        return {
            statementDate: `${year}-${month}-${String(days).padStart(2, '0')}`,
            periodFrom: `${year}${month}01`,
            periodTo: `${year}${month}${String(days).padStart(2, '0')}`,
            sortCode: requestedSortCode,
            customer: { customerId: customer.customerId, firstName: customer.firstName, lastName: customer.lastName, address: clone(customer.address), phoneNumber: customer.phoneNumber },
            account: {
                ...this.publicAccount(account),
                interestRate: account.interestRate,
                overdraftLimit: account.overdraftLimit
            },
            transactions: rows.map(clone),
            emptyHistoryMessage: rows.length ? null : 'NO TRANSACTIONS FOR THIS PERIOD',
            summary: {
                transactionCount: rows.length,
                totalCredits: credits,
                totalDebits: debits,
                closingBalance: account.availableBalance,
                openingBalance: money(account.availableBalance - credits + debits),
                availableBalance: account.availableBalance
            },
            footer: 'END OF STATEMENT'
        };
    }
}

module.exports = { LegacyBankSimulator, LegacySimulationError };
