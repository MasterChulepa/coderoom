import { Component, ElementRef, afterNextRender, input, output, signal, viewChild } from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';

@Component({
  selector: 'app-create-file-form',
  imports: [HlmButton],
  template: `
    <form class="grid gap-2 px-2 pb-3" (submit)="$event.preventDefault(); submit(name.value)"
      (keydown.escape)="cancel()">
      <label for="new-file-name" class="text-sm">Имя файла</label>
      <input
        #name
        id="new-file-name"
        class="box-border min-w-0 w-full rounded-md border border-solid border-input bg-background px-2 py-1 text-sm text-foreground"
        type="text"
        maxlength="500"
        required
        autocomplete="off"
        spellcheck="false"
        placeholder="main.ts"
        [disabled]="submitting()"
        (input)="validationError.set(null)"
      />
      @if (validationError() ?? error(); as message) {
        <p class="m-0 text-sm text-destructive" role="alert">{{ message }}</p>
      }
      <div class="flex flex-wrap gap-2">
        <button hlmBtn size="sm" type="submit" [disabled]="submitting()">
          {{ submitting() ? 'Создаём…' : 'Создать' }}
        </button>
        <button hlmBtn variant="ghost" size="sm" type="button" [disabled]="submitting()" (click)="cancel()">
          Отмена
        </button>
      </div>
    </form>
  `,
})
export class CreateFileFormComponent {
  readonly submitting = input.required<boolean>();
  readonly error = input<string | null>(null);
  readonly createRequested = output<string>();
  readonly cancelled = output<void>();
  protected readonly validationError = signal<string | null>(null);
  private readonly name = viewChild.required<ElementRef<HTMLInputElement>>('name');

  constructor() {
    afterNextRender(() => this.name().nativeElement.focus());
  }

  protected submit(value: string): void {
    if (this.submitting()) return;
    const name = value.trim();
    if (!name || name === '.' || name === '..' || /[\\/]/.test(name)) {
      this.validationError.set('Введи имя файла без пути.');
      return;
    }
    this.validationError.set(null);
    this.createRequested.emit(name);
  }

  protected cancel(): void {
    if (!this.submitting()) this.cancelled.emit();
  }
}
