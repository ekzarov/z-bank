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
