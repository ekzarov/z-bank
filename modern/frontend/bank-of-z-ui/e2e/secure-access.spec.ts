import { expect, test } from '@playwright/test';

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
