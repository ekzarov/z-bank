import { expect, test } from '@playwright/test';

test('web edge exposes real health, runtime identity, and security headers @e2e @operations', async ({ request }) => {
  const live = await request.get('health/live');
  const ready = await request.get('health/ready');
  const identity = await request.get('api/configuration/bank');
  const shell = await request.get('');

  expect(live.ok()).toBeTruthy();
  expect(ready.ok()).toBeTruthy();
  await expect(identity).toBeOK();
  expect(await identity.json()).toEqual({ displayName: 'Bank of Z', sortCode: '100000' });
  expect(shell.headers()['x-content-type-options']).toBe('nosniff');
  expect(shell.headers()['x-frame-options']).toBe('DENY');
  expect(shell.headers()['content-security-policy']).toContain("default-src 'self'");
});

test('sign-in stylesheet is active and the panel stays centered @e2e @operations @visual', async ({ page }) => {
  await page.goto('sign-in');

  const panel = page.locator('form.panel');
  await expect(panel).toBeVisible();
  await expect(panel).toHaveCSS('max-width', '520px');
  await expect(panel).toHaveCSS('background-color', 'rgb(255, 255, 255)');

  const assertPanelGeometry = async () => {
    const box = await panel.boundingBox();
    const viewport = page.viewportSize();
    expect(box).not.toBeNull();
    expect(viewport).not.toBeNull();
    expect(box!.x).toBeGreaterThanOrEqual(16);
    expect(box!.width).toBeLessThanOrEqual(520);
    expect(Math.abs(box!.x + box!.width / 2 - viewport!.width / 2)).toBeLessThanOrEqual(2);

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth
    );
    expect(hasHorizontalOverflow).toBeFalsy();
  };

  const globalStylesheet = page.locator('link[rel="stylesheet"][href*="styles-"]');
  await expect(globalStylesheet).toHaveCount(1);
  expect(await globalStylesheet.getAttribute('media')).not.toBe('print');

  await assertPanelGeometry();
  await page.setViewportSize({ width: 390, height: 844 });
  await assertPanelGeometry();
});
