import { Component, OnDestroy, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { HlmButton } from '@spartan-ng/helm/button';
import { ProjectFilesClient, type ProjectFileDto } from '../api/files/project-files.client';
import { CollaborativeDocumentConnectionService } from './collaboration/collaborative-document-connection.service';
import { CollaborativeEditorComponent } from './editor/collaborative-editor.component';
import { FileTreeComponent } from './files/file-tree.component';

interface FileSelection {
  projectId: number;
  file: ProjectFileDto;
}

@Component({
  selector: 'app-workspace-page',
  imports: [CollaborativeEditorComponent, FileTreeComponent, HlmButton],
  templateUrl: './workspace-page.component.html',
  styleUrl: './workspace-page.component.css',
})
export class WorkspacePageComponent implements OnDestroy {
  protected readonly connection = inject(CollaborativeDocumentConnectionService);
  private readonly filesClient = inject(ProjectFilesClient);
  private readonly router = inject(Router);
  protected readonly projectId = signal<number | null>(null);
  protected readonly files = signal<readonly ProjectFileDto[]>([]);
  protected readonly loadingFiles = signal(false);
  protected readonly filesError = signal<string | null>(null);
  protected readonly selection = signal<FileSelection | null>(null);
  protected readonly opened = signal<FileSelection | null>(null);
  protected readonly switchingFile = signal(false);
  protected readonly fileError = signal<string | null>(null);

  constructor() {
    inject(ActivatedRoute).paramMap.pipe(
      map(params => Number(params.get('projectId'))),
      distinctUntilChanged(),
      switchMap(projectId => {
        this.projectId.set(this.isValidId(projectId) ? projectId : null);
        this.files.set([]);
        this.filesError.set(null);
        this.selection.set(null);
        if (this.opened() || this.switchingFile()) void this.switchDocument();

        if (!this.isValidId(projectId)) {
          return of({ files: [], error: 'Укажи положительный целый ID проекта.' });
        }
        this.loadingFiles.set(true);
        return this.filesClient.list(projectId).pipe(
          map(files => ({ files, error: null })),
          catchError(() => of({ files: [], error: 'Не удалось загрузить файлы проекта.' })),
        );
      }),
      takeUntilDestroyed(),
    ).subscribe(({ files, error }) => {
      this.files.set(files);
      this.filesError.set(error);
      this.loadingFiles.set(false);
    });
  }

  protected openProject(projectId: number): void {
    if (this.isValidId(projectId)) void this.router.navigate(['/projects', projectId]);
  }

  protected selectFile(fileId: number): void {
    const projectId = this.projectId();
    const file = this.files().find(entry => entry.id === fileId);
    if (projectId === null || !file) return;
    const selected = this.selection();
    if (selected?.file.id === fileId && (this.switchingFile() || this.connection.status() === 'connected')) return;
    this.selection.set({ projectId, file });
    void this.switchDocument();
  }

  private async switchDocument(): Promise<void> {
    if (this.switchingFile()) return;
    this.switchingFile.set(true);
    this.opened.set(null);
    this.fileError.set(null);
    try {
      // Keep just the latest choice while a Hub connection is being opened.
      let target: FileSelection | null;
      do {
        target = this.selection();
        try {
          if (target) await this.connection.connect(target.projectId, target.file.id);
          else await this.connection.disconnect();
        } catch {
          if (target === this.selection()) this.fileError.set('Не удалось открыть документ.');
        }
      } while (target !== this.selection());

      if (target && this.connection.status() === 'connected') this.opened.set(target);
    } finally {
      this.switchingFile.set(false);
    }
  }

  private isValidId(value: number): boolean {
    return Number.isInteger(value) && value > 0 && value <= 2_147_483_647;
  }

  ngOnDestroy(): void {
    this.selection.set(null);
    if (this.opened() || this.switchingFile()) void this.switchDocument();
  }
}
