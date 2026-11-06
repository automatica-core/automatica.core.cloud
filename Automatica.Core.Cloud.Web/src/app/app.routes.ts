import { Routes } from '@angular/router';
import { adminGuard, authGuard, sysAdminGuard } from './core/auth';
import { Shell } from './shell';

export const routes: Routes = [
  { path: 'login', title: 'Sign in · Automatica Cloud', loadComponent: () => import('./pages/login').then((m) => m.LoginPage) },
  {
    path: 'verify-email',
    title: 'Confirm address · Automatica Cloud',
    loadComponent: () => import('./pages/verify-email').then((m) => m.VerifyEmailPage),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'servers' },
      { path: 'servers', title: 'Servers · Automatica Cloud', loadComponent: () => import('./pages/servers').then((m) => m.ServersPage) },
      {
        path: 'licenses',
        title: 'Licenses · Automatica Cloud',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/licenses').then((m) => m.LicensesPage),
      },
      {
        path: 'plugins',
        title: 'Plugins · Automatica Cloud',
        canActivate: [sysAdminGuard],
        loadComponent: () => import('./pages/plugins').then((m) => m.PluginsPage),
      },
      {
        path: 'versions',
        title: 'Versions · Automatica Cloud',
        canActivate: [sysAdminGuard],
        loadComponent: () => import('./pages/versions').then((m) => m.VersionsPage),
      },
      {
        path: 'deploy-keys',
        title: 'Deploy keys · Automatica Cloud',
        canActivate: [sysAdminGuard],
        loadComponent: () => import('./pages/deploy-keys').then((m) => m.DeployKeysPage),
      },
      {
        path: 'users',
        title: 'Users · Automatica Cloud',
        canActivate: [sysAdminGuard],
        loadComponent: () => import('./pages/users').then((m) => m.UsersPage),
      },
      { path: 'account', title: 'Account · Automatica Cloud', loadComponent: () => import('./pages/account').then((m) => m.AccountPage) },
    ],
  },
  { path: '**', redirectTo: '' },
];
