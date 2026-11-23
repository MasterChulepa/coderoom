import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface ProjectFileDto {
  readonly id: number;
  readonly projectId: number;
  readonly path: string;
  readonly content: string;
}

@Injectable({ providedIn: 'root' })
export class ProjectFilesClient {
  private readonly http = inject(HttpClient);

  list(projectId: number) {
    return this.http.get<readonly ProjectFileDto[]>(`/api/projects/${projectId}/files`);
  }

  create(projectId: number, path: string) {
    return this.http.post<ProjectFileDto>(`/api/projects/${projectId}/files`, {
      path,
      content: '',
    });
  }
}
