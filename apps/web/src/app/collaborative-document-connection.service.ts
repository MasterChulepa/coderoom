import { Injectable, NgZone, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class CollaborativeDocumentConnectionService {
  readonly status = signal<'disconnected' | 'connecting' | 'connected'>('disconnected');
  readonly initialUpdateCount = signal(0);
  readonly receivedUpdateCount = signal(0);
  readonly error = signal<string | null>(null);

  private readonly zone = inject(NgZone);
  private connection: HubConnection | null = null;

  async connect(projectId: number, fileId: number): Promise<void> {
    if (this.status() === 'connecting') return;

    await this.disconnect();
    this.status.set('connecting');
    this.error.set(null);
    this.initialUpdateCount.set(0);
    this.receivedUpdateCount.set(0);

    const connection = new HubConnectionBuilder().withUrl('/hubs/projects').build();
    this.connection = connection;

    // Subscribe before starting, so updates arriving after JoinFile are observed.
    connection.on('DocumentUpdateReceived', (updatedProjectId: number, updatedFileId: number, _update: string) => {
      if (updatedProjectId === projectId && updatedFileId === fileId) {
        this.zone.run(() => this.receivedUpdateCount.update(count => count + 1));
      }
    });

    connection.onclose(error => {
      if (this.connection === connection) {
        this.zone.run(() => {
          this.status.set('disconnected');
          if (error) this.error.set(error.message);
        });
      }
    });

    try {
      await connection.start();
      await connection.invoke('JoinFile', projectId, fileId);
      const updates = await connection.invoke<string[]>('GetCollaborativeDocumentUpdatesAsync', projectId, fileId);
      this.initialUpdateCount.set(updates.length); // An empty document returns [].
      this.status.set('connected');
      console.info(`CodeRoom: received ${updates.length} existing updates for file ${fileId}.`);
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : String(error));
      this.status.set('disconnected');
      this.connection = null;
      await connection.stop();
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.status.set('disconnected');
    if (connection) await connection.stop();
  }
}
