import { expect, test } from '@playwright/test';

function requiredDemoPassword(): string {
  const password = process.env['BANKOFZ_DEMO_PASSWORD'];
  if (!password) {
    throw new Error('BANKOFZ_DEMO_PASSWORD is required; credentialed E2E tests must not be skipped.');
  }
  return password;
}

const roleCases = [
  { userName: 'customer', link: 'My banking', heading: 'My profile', hiddenLinks: ['Customer operations', 'Administration'] },
  { userName: 'operator', link: 'Customer operations', heading: 'Customer workspace', hiddenLinks: ['My banking', 'Administration'] },
  { userName: 'administrator', link: 'Administration', heading: 'Access administration', hiddenLinks: ['My banking', 'Customer operations'] }
];

test('customer can sign in, use a protected route, and sign out @e2e', async ({ page }) => {
  const password = requiredDemoPassword();

  await page.goto('customer');
  await expect(page).toHaveURL(/\/z-bank-new\/sign-in$/);
  await page.getByLabel('User name').fill('customer');
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.getByRole('heading', { name: 'Welcome to Bank of Z' })).toBeVisible();
  await page.getByRole('link', { name: 'My banking' }).click();
  await expect(page.getByRole('heading', { name: 'My profile' })).toBeVisible();

  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page).toHaveURL(/\/z-bank-new\/sign-in$/);
  await page.goto('customer');
  await expect(page).toHaveURL(/\/z-bank-new\/sign-in$/);
});

test('API outage opens the recoverable unavailable page @e2e', async ({ page }) => {
  await page.route('**/api/session', route => route.fulfill({
    status: 503,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title: 'Service Unavailable', status: 503 })
  }));

  await page.goto('customer');

  await expect(page).toHaveURL(/\/z-bank-new\/unavailable$/);
  await expect(page.getByRole('heading', { name: 'We could not complete that request' })).toBeVisible();
});

test('customer can transfer between owned demo accounts and see a rejected transfer @e2e @funds-transfers', async ({ page }) => {
  const password = requiredDemoPassword();
  await page.goto('sign-in');
  await page.getByLabel('User name').fill('customer');
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.getByRole('link', { name: 'My banking' }).click();
  await page.getByRole('link', { name: /10000000/ }).click();

  await page.getByLabel('Operation').selectOption('deposit');
  await page.locator('.cash-panel').getByLabel('Amount').fill('10.00');
  await page.getByRole('button', { name: 'Book operation' }).click();
  await expect(page.getByRole('status')).toContainText(/Deposit [0-9a-f]{32} booked/);

  await page.getByLabel('Destination account').fill('10000099');
  await page.locator('.transfer-panel').getByLabel('Amount').fill('5.00');
  await page.getByRole('button', { name: 'Transfer', exact: true }).click();
  await expect(page.getByRole('status')).toContainText(/Transfer [0-9a-f]{32} booked/);

  await page.getByLabel('Destination account').fill('10000099');
  await page.locator('.transfer-panel').getByLabel('Amount').fill('999999.00');
  await page.getByRole('button', { name: 'Transfer', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('exceeds the available balance');
});

for (const roleCase of roleCases) {
  test(`${roleCase.userName} sees only authorized navigation @e2e`, async ({ page }) => {
    const password = requiredDemoPassword();

    await page.goto('sign-in');
    await page.getByLabel('User name').fill(roleCase.userName);
    await page.getByLabel('Password').fill(password);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await page.getByRole('link', { name: roleCase.link }).click();
    await expect(page.getByRole('heading', { name: roleCase.heading })).toBeVisible();
    for (const hiddenLink of roleCase.hiddenLinks) {
      await expect(page.getByRole('link', { name: hiddenLink })).toHaveCount(0);
    }
  });
}

test('operator can create find update and retire a customer @e2e', async ({ page }) => {
  const password = requiredDemoPassword();

  await page.goto('sign-in');
  await page.getByLabel('User name').fill('operator');
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.getByRole('link', { name: 'Customer operations' }).click();

  await page.getByRole('button', { name: 'New customer' }).click();
  await page.getByLabel('First name').fill('Taylor');
  await page.getByLabel('Last name').fill('Parity');
  await page.getByLabel('Date of birth').fill('1990-06-15');
  await page.getByLabel('Address', { exact: true }).fill('24 Migration Road');
  await page.getByLabel('City').fill('London');
  await page.getByLabel('Postal code').fill('EC1A 1AA');
  await page.getByLabel('Email').fill(`taylor.${Date.now()}@example.test`);
  await page.getByRole('button', { name: 'Save' }).click();

  const saved = page.getByRole('status');
  await expect(saved).toContainText(/Customer \d{10} saved/);
  const customerId = (await saved.textContent())!.match(/\d{10}/)![0];

  await page.getByLabel('Customer ID or name').fill(customerId);
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByText(customerId, { exact: true }).first()).toBeVisible();

  await page.getByLabel('Last name').fill('Verified');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByRole('status')).toContainText(`Customer ${customerId} saved`);

  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Retire' }).click();
  await expect(page.getByRole('status')).toContainText(`Customer ${customerId} retired`);
});

test('operator can manage an account and book cash with insufficient-funds protection @e2e @account-management @cash-transactions', async ({ page }) => {
  const password = requiredDemoPassword();

  await page.goto('sign-in');
  await page.getByLabel('User name').fill('operator');
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.getByRole('link', { name: 'Customer operations' }).click();
  await page.getByLabel('Customer ID or name').fill('1000000001');
  await page.getByRole('button', { name: 'Search' }).click();

  await page.getByRole('button', { name: 'New account' }).click();
  await page.getByLabel('Product').selectOption('saving');
  await page.getByLabel('Interest rate').fill('2.75');
  await page.getByRole('button', { name: 'Create' }).click();
  const status = page.getByRole('status');
  await expect(status).toContainText(/Account \d{8} created/);
  const accountId = (await status.textContent())!.match(/\d{8}/)![0];

  await page.getByRole('link', { name: new RegExp(accountId) }).click();
  await expect(page.getByRole('heading', { name: new RegExp(accountId) })).toBeVisible();

  await page.getByLabel('Operation').selectOption('deposit');
  await page.locator('.cash-panel').getByLabel('Amount').fill('125.00');
  await page.getByRole('button', { name: 'Book operation' }).click();
  await expect(page.getByRole('status')).toContainText(/Deposit [0-9a-f]{32} booked/);
  await expect(page.locator('.balance-band')).toContainText('£125.00');

  await page.getByLabel('Operation').selectOption('withdrawal');
  await page.locator('.cash-panel').getByLabel('Amount').fill('1000.00');
  await page.getByRole('button', { name: 'Book operation' }).click();
  await expect(page.getByRole('alert')).toContainText('exceeds the available balance');
  await expect(page.locator('.balance-band')).toContainText('£125.00');

  await page.locator('.cash-panel').getByLabel('Amount').fill('125.00');
  await page.getByRole('button', { name: 'Book operation' }).click();
  await expect(page.getByRole('status')).toContainText(/Withdrawal [0-9a-f]{32} booked/);
  await expect(page.locator('.balance-band')).toContainText('£0.00');

  await page.getByRole('button', { name: 'Edit terms' }).click();
  await page.getByLabel('Interest rate').fill('3.25');
  await page.getByRole('button', { name: 'Save terms' }).click();
  await expect(page.getByRole('status')).toContainText('Account terms updated');

  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Close account' }).click();
  await expect(page.getByRole('status')).toContainText('Account closed');
  await expect(page.getByText('closed', { exact: true })).toBeVisible();
});

test('operator can transfer between accounts and rejected transfer keeps the balance @e2e @funds-transfers', async ({ page }) => {
  const password = requiredDemoPassword();

  await page.goto('sign-in');
  await page.getByLabel('User name').fill('operator');
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByRole('heading', { name: 'Welcome to Bank of Z' })).toBeVisible();
  const sourceId = '10000000';
  const destinationId = '10000099';
  await page.goto(`accounts/${sourceId}`);

  await page.getByLabel('Operation').selectOption('deposit');
  await page.locator('.cash-panel').getByLabel('Amount').fill('100.00');
  await page.getByRole('button', { name: 'Book operation' }).click();
  await expect(page.getByRole('status')).toContainText(/Deposit [0-9a-f]{32} booked/);

  await page.getByLabel('Destination account').fill(destinationId);
  await page.locator('.transfer-panel').getByLabel('Amount').fill('25.00');
  await page.getByRole('button', { name: 'Transfer', exact: true }).click();
  await expect(page.getByRole('status')).toContainText(/Transfer [0-9a-f]{32} booked/);
  const balanceAfterTransfer = await page.locator('.balance-band').textContent();

  await page.getByLabel('Destination account').fill(destinationId);
  await page.locator('.transfer-panel').getByLabel('Amount').fill('1000.00');
  await page.getByRole('button', { name: 'Transfer', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('exceeds the available balance');
  await expect(page.locator('.balance-band')).toHaveText(balanceAfterTransfer!);
});
