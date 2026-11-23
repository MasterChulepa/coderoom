import type { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'projects/1' },
  {
    path: 'projects/:projectId',
    loadComponent: () =>
      import('./workspace/workspace-page.component').then(module => module.WorkspacePageComponent),
  },
];
