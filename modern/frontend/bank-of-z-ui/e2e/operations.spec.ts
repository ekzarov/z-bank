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
