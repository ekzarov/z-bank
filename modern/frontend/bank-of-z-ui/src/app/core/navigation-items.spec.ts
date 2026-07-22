import { describe, expect, it } from 'vitest';
import { navigationForRoles } from './navigation-items';

describe('navigationForRoles', () => {
  it.each([
    ['Customer', '/customer'],
    ['Operator', '/operations'],
    ['Administrator', '/administration']
  ])('shows only the workspace for %s', (role, expectedPath) => {
    const paths = navigationForRoles([role]).map(item => item.path);

    expect(paths).toContain('/');
    expect(paths).toContain(expectedPath);
    expect(paths).toHaveLength(2);
  });

  it('does not expose staff workspaces to a customer', () => {
    const paths = navigationForRoles(['Customer']).map(item => item.path);

    expect(paths).not.toContain('/operations');
    expect(paths).not.toContain('/administration');
  });
});
