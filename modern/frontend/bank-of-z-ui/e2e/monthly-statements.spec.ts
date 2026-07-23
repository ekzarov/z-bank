import { expect, test } from '@playwright/test';

function requiredDemoPassword(): string {
  const password = process.env['BANKOFZ_DEMO_PASSWORD'];
  if (!password) {
    throw new Error('BANKOFZ_DEMO_PASSWORD is required; credentialed E2E tests must not be skipped.');
  }
  return password;
}

async function signIn(page: import('@playwright/test').Page, userName: string): Promise<void> {
  await page.goto('sign-in');
  await page.getByLabel('User name').fill(userName);
  await page.getByLabel('Password').fill(requiredDemoPassword());
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByRole('heading', { name: 'Welcome to Bank of Z' })).toBeVisible();
}

test('customer generates and views a populated monthly statement @e2e @monthly-statements', async ({ page }) => {
  await signIn(page, 'customer');
  await page.goto('accounts/10000000');
  const cash = page.getByRole('region', { name: 'Deposit or withdraw' });
  await cash.getByLabel('Operation').selectOption('deposit');
  await cash.getByLabel('Amount').fill('1.00');
  await cash.getByRole('button', { name: 'Book operation' }).click();
  await expect(page.getByRole('status')).toContainText('booked. Available balance');

  await page.goto('accounts/10000000/statements');
  await page.getByLabel('Year').fill('2026');
  await page.getByLabel('Month').selectOption({ label: 'July' });
  await page.getByRole('button', { name: 'Generate statement' }).click();

  await expect(page).toHaveURL(/\/accounts\/10000000\/statements\/[0-9a-f-]{36}$/);
  await expect(page.getByRole('heading', { name: 'Monthly account statement' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Statement summary' })).toContainText('Closing balance');
  await expect(page.getByRole('region', { name: 'Statement transactions' }).locator('tbody tr').first()).toBeVisible();
  await expect(page.getByRole('button', { name: 'Print statement' })).toBeVisible();
});

test('operator sees partial bulk outcomes and retries only failures @e2e @monthly-statements @bulk-statements', async ({ page }) => {
  await signIn(page, 'operator');
  const requests: Array<{ accountIds: string[] | null }> = [];
  let attempt = 0;
  await page.route('**/api/statements/bulk', async route => {
    requests.push(JSON.parse(route.request().postData() ?? '{}'));
    attempt += 1;
    const accounts = attempt === 1
      ? [
          { accountId: '10000001', succeeded: true, generationId: '11111111-1111-1111-1111-111111111111', reused: true, error: null },
          { accountId: '10000002', succeeded: false, generationId: null, reused: false, error: 'Source data unavailable' }
        ]
      : [
          { accountId: '10000002', succeeded: true, generationId: '22222222-2222-2222-2222-222222222222', reused: false, error: null }
        ];
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        year: 2026,
        month: 6,
        total: accounts.length,
        succeeded: accounts.filter(account => account.succeeded).length,
        failed: accounts.filter(account => !account.succeeded).length,
        accounts
      })
    });
  });

  await page.getByRole('link', { name: 'Statements' }).click();
  await expect(page.getByRole('heading', { name: 'Monthly statement runs' })).toBeVisible();
  await page.getByLabel('Year').fill('2026');
  await page.getByLabel('Month').selectOption({ label: 'June' });
  await page.getByRole('button', { name: 'Run statement batch' }).click();
  await expect(page.getByText('Source data unavailable')).toBeVisible();
  await expect(page.getByText('Reused')).toBeVisible();

  await page.getByRole('button', { name: 'Retry 1 failed' }).click();
  await expect(page.getByText('Source data unavailable')).toHaveCount(0);
  expect(requests).toEqual([
    { year: 2026, month: 6, accountIds: null },
    { year: 2026, month: 6, accountIds: ['10000002'] }
  ]);
});
