import { Injectable, NgZone, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import * as Y from 'yjs';

const BASE64_ENCODING_CHUNK_SIZE = 8192;

@Injectable({ providedIn: 'root' })
export class CollaborativeDocumentConnectionService {
  readonly status = signal<'disconnected' | 'connecting' | 'connected'>('disconnected');
  readonly initialUpdateCount = signal(0);
  readonly receivedUpdateCount = signal(0);
  readonly documentContent = signal('');
  readonly error = signal<string | null>(null);

  document: Y.Doc | null = null;
  text: Y.Text | null = null;

  private readonly zone = inject(NgZone);
  private readonly localEditOrigin = Symbol('local-edit');
  private readonly remoteUpdateOrigin = Symbol('remote-update');
  private connection: HubConnection | null = null;
  private connecting = false;

  async connect(projectId: number, fileId: number): Promise<void> {
    if (this.connecting) return;
    this.connecting = true;

    try {
      await this.disconnect();
      this.status.set('connecting');
      this.error.set(null);
      this.initialUpdateCount.set(0);
      this.receivedUpdateCount.set(0);

      const document = new Y.Doc();
      const text = document.getText('content');
      text.observe(() => this.zone.run(() => this.documentContent.set(text.toString())));
      this.document = document;
      this.text = text;

      const connection = new HubConnectionBuilder().withUrl('/hubs/projects').build();
      this.connection = connection;

      document.on('update', (update: Uint8Array, origin: unknown) => {
        if (origin !== this.localEditOrigin || this.connection !== connection) return;

        const updateId = crypto.randomUUID();
        const base64 = this.encodeUpdate(update);
        void connection.invoke('UpdateCollaborativeDocumentAsync', projectId, fileId, updateId, base64)
          .catch(error => this.zone.run(() => {
            this.error.set(`Не удалось отправить правку: ${error instanceof Error ? error.message : String(error)}`);
          }));
      });

      // Subscribe before starting, so updates arriving after JoinFile are observed.
      connection.on('DocumentUpdateReceived', (updatedProjectId: number, updatedFileId: number, update: string) => {
        if (this.connection !== connection || updatedProjectId !== projectId || updatedFileId !== fileId) return;

        this.zone.run(() => {
          try {
            this.applyUpdate(document, update);
            this.receivedUpdateCount.update(count => count + 1);
          } catch (error) {
            this.error.set(error instanceof Error ? error.message : String(error));
          }
        });
      });

      connection.onclose(error => {
        if (this.connection === connection) {
          this.zone.run(() => {
            this.connection = null;
            this.resetDocument();
            this.status.set('disconnected');
            if (error) this.error.set(error.message);
          });
        }
      });

      try {
        await connection.start();
        if (this.connection !== connection) return;
        await connection.invoke('JoinFile', projectId, fileId);
        if (this.connection !== connection) return;
        const updates = await connection.invoke<string[]>('GetCollaborativeDocumentUpdatesAsync', projectId, fileId);
        if (this.connection !== connection) return;
        for (const update of updates) this.applyUpdate(document, update);
        this.initialUpdateCount.set(updates.length); // An empty document returns [].
        this.status.set('connected');
        console.info(`CodeRoom: received ${updates.length} existing updates for file ${fileId}.`);
      } catch (error) {
        this.error.set(error instanceof Error ? error.message : String(error));
        this.status.set('disconnected');
        this.connection = null;
        this.resetDocument();
        await connection.stop();
      }
    } finally {
      this.connecting = false;
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.resetDocument();
    this.status.set('disconnected');
    if (connection) await connection.stop();
  }

  updateText(nextContent: string): void {
    const document = this.document;
    const text = this.text;
    if (this.status() !== 'connected' || !document || !text) return;

    const currentContent = text.toString();
    if (nextContent === currentContent) return;

    let prefix = 0;
    while (prefix < currentContent.length && prefix < nextContent.length &&
      currentContent[prefix] === nextContent[prefix]) prefix++;

    let suffix = 0;
    while (suffix < currentContent.length - prefix && suffix < nextContent.length - prefix &&
      currentContent[currentContent.length - 1 - suffix] === nextContent[nextContent.length - 1 - suffix]) suffix++;

    const deletedLength = currentContent.length - prefix - suffix;
    const insertedText = nextContent.slice(prefix, nextContent.length - suffix);
    document.transact(() => {
      if (deletedLength > 0) text.delete(prefix, deletedLength);
      if (insertedText) text.insert(prefix, insertedText);
    }, this.localEditOrigin);
  }

  private applyUpdate(document: Y.Doc, base64: string): void {
    const binary = atob(base64);
    const update = Uint8Array.from(binary, char => char.charCodeAt(0));
    Y.applyUpdate(document, update, this.remoteUpdateOrigin);
  }

  private encodeUpdate(update: Uint8Array): string {
    const chunks: string[] = [];
    for (let offset = 0; offset < update.length; offset += BASE64_ENCODING_CHUNK_SIZE) {
      chunks.push(String.fromCharCode(...update.subarray(offset, offset + BASE64_ENCODING_CHUNK_SIZE)));
    }
    return btoa(chunks.join(''));
  }

  private resetDocument(): void {
    this.document?.destroy();
    this.document = null;
    this.text = null;
    this.documentContent.set('');
  }
}
