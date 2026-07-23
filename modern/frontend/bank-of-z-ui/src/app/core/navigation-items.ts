export interface NavigationItem {
  path: string;
  label: string;
  role?: string;
}

const items: NavigationItem[] = [
  { path: '/', label: 'Overview' },
  { path: '/customer', label: 'My banking', role: 'Customer' },
  { path: '/operations', label: 'Customer operations', role: 'Operator' },
  { path: '/operations/statements', label: 'Statements', role: 'Operator' },
  { path: '/administration', label: 'Administration', role: 'Administrator' }
];

export function navigationForRoles(roles: string[]): NavigationItem[] {
  return items.filter(item => !item.role || roles.includes(item.role));
}
