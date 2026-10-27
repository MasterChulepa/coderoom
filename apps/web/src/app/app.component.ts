import { Component, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CollaborativeDocumentConnectionService } from './collaborative-document-connection.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnDestroy {
  readonly connection = inject(CollaborativeDocumentConnectionService);

  projectId = 1;
  fileId = 1;

  async connect(): Promise<void> {
    if (!this.isValidId(this.projectId) || !this.isValidId(this.fileId)) return;
    await this.connection.connect(this.projectId, this.fileId);
  }

  isValidId(value: number): boolean {
    return Number.isInteger(value) && value > 0;
  }

  ngOnDestroy(): void {
    void this.connection.disconnect();
  }
}
