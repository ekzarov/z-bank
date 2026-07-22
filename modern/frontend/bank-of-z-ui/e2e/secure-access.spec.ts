import { expect, test } from '@playwright/test';

const roleCases = [
  { userName: 'customer', link: 'My banking', heading: 'My banking', hiddenLinks: ['Customer operations', 'Administration'] },
  { userName: 'operator', link: 'Customer operations', heading: 'Customer operations', hiddenLinks: ['My banking', 'Administration'] },
  { userName: 'administrator', link: 'Administration', heading: 'Access administration', hiddenLinks: ['My banking', 'Customer operations'] }
];

test('customer can sign in, use a protected route, and sign out @e2e', async ({ page }) => {
  const password = process.env['BANKOFZ_DEMO_PASSWORD'];
  test.skip(!password, 'BANKOFZ_DEMO_PASSWORD is required.');

  await page.goto('customer');
  await expect(page).toHaveURL(/\/z-bank-new\/sign-in$/);
  await page.getByLabel('User name').fill('customer');
  await page.getByLabel('Password').fill(password!);
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.getByRole('heading', { name: 'Welcome to Bank of Z' })).toBeVisible();
  await page.getByRole('link', { name: 'My banking' }).click();
  await expect(page.getByRole('heading', { name: 'My banking' })).toBeVisible();

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

for (const roleCase of roleCases) {
  test(`${roleCase.userName} sees only authorized navigation @e2e`, async ({ page }) => {
    const password = process.env['BANKOFZ_DEMO_PASSWORD'];
    test.skip(!password, 'BANKOFZ_DEMO_PASSWORD is required.');

    await page.goto('sign-in');
    await page.getByLabel('User name').fill(roleCase.userName);
    await page.getByLabel('Password').fill(password!);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await page.getByRole('link', { name: roleCase.link }).click();
    await expect(page.getByRole('heading', { name: roleCase.heading })).toBeVisible();
    for (const hiddenLink of roleCase.hiddenLinks) {
      await expect(page.getByRole('link', { name: hiddenLink })).toHaveCount(0);
    }
  });
}

test('operator can create find update and retire a customer @e2e', async ({ page }) => {
  const password = process.env['BANKOFZ_DEMO_PASSWORD'];
  test.skip(!password, 'BANKOFZ_DEMO_PASSWORD is required.');

  await page.goto('sign-in');
  await page.getByLabel('User name').fill('operator');
  await page.getByLabel('Password').fill(password!);
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
