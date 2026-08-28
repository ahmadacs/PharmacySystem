import { Routes } from '@angular/router';
import { Permissions } from './core/constants/permissions';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then((m) => m.ResetPasswordComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'medicines',
        canActivate: [permissionGuard(Permissions.MedicinesView)],
        loadComponent: () =>
          import('./features/medicines/medicines-list/medicines-list.component').then((m) => m.MedicinesListComponent)
      },
      {
        path: 'inventory',
        canActivate: [permissionGuard(Permissions.InventoryView)],
        loadComponent: () => import('./features/inventory/inventory/inventory.component').then((m) => m.InventoryComponent)
      },
      {
        path: 'prescriptions',
        canActivate: [permissionGuard(Permissions.PrescriptionsView, Permissions.PrescriptionsManageOwn)],
        loadComponent: () =>
          import('./features/prescriptions/prescriptions-list/prescriptions-list.component').then(
            (m) => m.PrescriptionsListComponent
          )
      },
      {
        path: 'dispensing',
        canActivate: [permissionGuard(Permissions.DispensingView)],
        loadComponent: () =>
          import('./features/dispensing/dispensing-list/dispensing-list.component').then(
            (m) => m.DispensingListComponent
          )
      },
      {
        path: 'users',
        canActivate: [permissionGuard(Permissions.UsersManage)],
        loadComponent: () => import('./features/users/users-list/users-list.component').then((m) => m.UsersListComponent)
      },
      {
        path: 'audit-log',
        canActivate: [permissionGuard(Permissions.AuditLogView)],
        loadComponent: () =>
          import('./features/audit-log/audit-log-list/audit-log-list.component').then((m) => m.AuditLogListComponent)
      },
      {
        path: 'forbidden',
        loadComponent: () => import('./layout/forbidden/forbidden.component').then((m) => m.ForbiddenComponent)
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  },
  {
    path: 'not-found',
    loadComponent: () => import('./layout/not-found/not-found.component').then((m) => m.NotFoundComponent)
  },
  { path: '**', redirectTo: 'not-found' }
];