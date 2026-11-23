import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnDestroy, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { HlmButton } from '@spartan-ng/helm/button';
import { ProjectFilesClient, type ProjectFileDto } from '../api/files/project-files.client';
import { CollaborativeDocumentConnectionService } from './collaboration/collaborative-document-connection.service';
import { CollaborativeEditorComponent } from './editor/collaborative-editor.component';
import { CreateFileFormComponent } from './files/create-file-form.component';
import { FileTreeComponent } from './files/file-tree.component';

interface FileSelection {
  projectId: number;
  file: ProjectFileDto;
}

interface CreateFileState {
  readonly submitting: boolean;
  readonly error: string | null;
}

@Component({
  selector: 'app-workspace-page',
  imports: [CollaborativeEditorComponent, CreateFileFormComponent, FileTreeComponent, HlmButton],
  templateUrl: './workspace-page.component.html',
  styleUrl: './workspace-page.component.css',
})
export class WorkspacePageComponent implements OnDestroy {
  protected readonly connection = inject(CollaborativeDocumentConnectionService);
  private readonly filesClient = inject(ProjectFilesClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly projectId = signal<number | null>(null);
  protected readonly files = signal<readonly ProjectFileDto[]>([]);
  protected readonly loadingFiles = signal(false);
  protected readonly filesError = signal<string | null>(null);
  protected readonly selection = signal<FileSelection | null>(null);
  protected readonly opened = signal<FileSelection | null>(null);
  protected readonly switchingFile = signal(false);
  protected readonly fileError = signal<string | null>(null);
  protected readonly createFileState = signal<CreateFileState | null>(null);

  constructor() {
    inject(ActivatedRoute).paramMap.pipe(
      map(params => Number(params.get('projectId'))),
      distinctUntilChanged(),
      switchMap(projectId => {
        this.projectId.set(this.isValidId(projectId) ? projectId : null);
        this.files.set([]);
        this.filesError.set(null);
        this.createFileState.set(null);
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

  protected toggleCreateFile(): void {
    if (this.createFileState()?.submitting || this.loadingFiles() || this.projectId() === null) return;
    this.createFileState.update(state => state ? null : { submitting: false, error: null });
  }

  protected createFile(name: string): void {
    const projectId = this.projectId();
    const state = this.createFileState();
    if (projectId === null || !state || state.submitting) return;

    const request: CreateFileState = { submitting: true, error: null };
    this.createFileState.set(request);
    this.filesClient.create(projectId, name).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: file => {
        if (this.createFileState() !== request) return;
        this.files.update(files => files.some(entry => entry.id === file.id) ? files : [...files, file]);
        this.createFileState.set(null);
      },
      error: (error: unknown) => {
        if (this.createFileState() !== request) return;
        let message = 'Не удалось создать файл. Попробуй ещё раз.';
        if (error instanceof HttpErrorResponse) {
          if (error.status === 409) message = 'Файл с таким именем уже существует.';
          if (error.status === 404) message = 'Проект не найден.';
        }
        this.createFileState.set({ submitting: false, error: message });
      },
    });
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
