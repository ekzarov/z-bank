import { describe, expect, it } from 'vitest';
import { navigationForRoles } from './navigation-items';

describe('navigationForRoles', () => {
  it.each([
    ['Customer', ['/customer']],
    ['Operator', ['/operations', '/operations/statements']],
    ['Administrator', ['/administration']]
  ])('shows only the workspaces for %s', (role, expectedPaths) => {
    const paths = navigationForRoles([role]).map(item => item.path);

    expect(paths).toContain('/');
    for (const expectedPath of expectedPaths) {
      expect(paths).toContain(expectedPath);
    }
    expect(paths).toHaveLength(expectedPaths.length + 1);
  });

  it('does not expose staff workspaces to a customer', () => {
    const paths = navigationForRoles(['Customer']).map(item => item.path);

    expect(paths).not.toContain('/operations');
    expect(paths).not.toContain('/administration');
  });
});
