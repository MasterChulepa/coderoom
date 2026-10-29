import { AfterViewInit, Component, ElementRef, Input, OnDestroy, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import type * as Monaco from 'monaco-editor';
import type { MonacoBinding } from 'y-monaco';
import type * as Y from 'yjs';

@Component({
  selector: 'app-collaborative-editor',
  template: '<div #container class="editor" aria-label="Содержимое файла"></div>',
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: ['.editor { width: 100%; height: 400px; border: 1px solid #ccc; }'],
})
export class CollaborativeEditorComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) text!: Y.Text;
  @ViewChild('container', { static: true }) private container!: ElementRef<HTMLElement>;

  private editor: Monaco.editor.IStandaloneCodeEditor | null = null;
  private model: Monaco.editor.ITextModel | null = null;
  private binding: MonacoBinding | null = null;
  private destroyed = false;

  async ngAfterViewInit(): Promise<void> {
    (globalThis as typeof globalThis & { MonacoEnvironment: { getWorker: () => Worker } }).MonacoEnvironment = {
      getWorker: () => new Worker(new URL('./monaco-editor.worker', import.meta.url), { type: 'module' }),
    };
    const [monaco, { MonacoBinding }] = await Promise.all([
      import('monaco-editor/esm/vs/editor/editor.api'),
      import('y-monaco'),
    ]);
    if (this.destroyed) return;

    this.model = monaco.editor.createModel('', 'plaintext');
    this.editor = monaco.editor.create(this.container.nativeElement, {
      model: this.model,
      automaticLayout: true,
      minimap: { enabled: false },
    });
    this.binding = new MonacoBinding(this.text, this.model, new Set([this.editor]));
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.binding?.destroy();
    this.editor?.dispose();
    this.model?.dispose();
  }
}
