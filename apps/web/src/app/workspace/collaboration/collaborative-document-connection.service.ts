import { Injectable, NgZone, inject, signal } from '@angular/core';
import * as Y from 'yjs';
import type { DocumentUpdatePayload } from '../../api/collaboration/collaboration.dto';
import { ProjectHubClient } from '../../api/collaboration/project-hub.client';

const BASE64_ENCODING_CHUNK_SIZE = 8192;
const PENDING_RETRY_INTERVAL_MS = 5000;
const MAX_UPDATE_SEND_ATTEMPTS = 3;

type PendingUpdate = DocumentUpdatePayload & {
  attempts: number;
  status: 'queued' | 'sending' | 'needsAttention';
};

@Injectable({ providedIn: 'root' })
export class CollaborativeDocumentConnectionService {
  readonly status = signal<'disconnected' | 'connecting' | 'connected'>('disconnected');
  readonly initialUpdateCount = signal(0);
  readonly receivedUpdateCount = signal(0);
  readonly documentContent = signal('');
  readonly error = signal<string | null>(null);

  document: Y.Doc | null = null;
  text: Y.Text | null = null;
  readonly pendingUpdates = new Map<string, PendingUpdate>();

  private readonly zone = inject(NgZone);
  private readonly localEditOrigin = Symbol('local-edit');
  private readonly remoteUpdateOrigin = Symbol('remote-update');
  private connection: ProjectHubClient | null = null;
  private readonly inFlightUpdates = new Map<string, ProjectHubClient>();
  private connecting = false;
  private retryTimer: ReturnType<typeof setInterval> | null = null;
  private idleRetry: number | null = null;

  async connect(projectId: number, fileId: number): Promise<void> {
    if (this.connecting) return;
    this.connecting = true;

    try {
      await this.disconnect();
      this.prepareConnectionState();
      const document = this.createDocument();
      const connection = this.createHubConnection();
      this.subscribeToLocalUpdates(document, connection, projectId, fileId);
      this.subscribeToRemoteUpdates(document, connection, projectId, fileId);
      this.subscribeToConnectionClose(connection);

      try {
        await this.startConnectionAndRestoreDocument(document, connection, projectId, fileId);
      } catch (error) {
        await this.handleConnectionFailure(connection, error);
      }
    } finally {
      this.connecting = false;
    }
  }

  private prepareConnectionState(): void {
    this.status.set('connecting');
    this.error.set(null);
    this.initialUpdateCount.set(0);
    this.receivedUpdateCount.set(0);
  }

  private createDocument(): Y.Doc {
    const document = new Y.Doc();
    const text = document.getText('content');
    text.observe(() => this.zone.run(() => this.documentContent.set(text.toString())));
    this.document = document;
    this.text = text;
    return document;
  }

  private createHubConnection(): ProjectHubClient {
    const connection = new ProjectHubClient();
    this.connection = connection;
    return connection;
  }

  private subscribeToLocalUpdates(document: Y.Doc, connection: ProjectHubClient, projectId: number, fileId: number): void {
    document.on('update', (update: Uint8Array, _origin: unknown, _doc: Y.Doc, transaction: Y.Transaction) => {
      if (!transaction.local || this.connection !== connection) return;

      const updateId = crypto.randomUUID();
      const base64 = this.encodeUpdate(update);
      const pendingUpdate: PendingUpdate = { projectId, fileId, updateId, base64, attempts: 0, status: 'queued' };
      this.pendingUpdates.set(updateId, pendingUpdate);
      void this.sendPendingUpdate(connection, pendingUpdate);
    });
  }

  private async sendPendingUpdate(connection: ProjectHubClient, pendingUpdate: PendingUpdate): Promise<void> {
    const { updateId } = pendingUpdate;
    if (this.connection !== connection || this.inFlightUpdates.get(updateId) === connection ||
      this.pendingUpdates.get(updateId) !== pendingUpdate || pendingUpdate.status === 'needsAttention') return;
    if (pendingUpdate.attempts >= MAX_UPDATE_SEND_ATTEMPTS) {
      pendingUpdate.status = 'needsAttention';
      return;
    }

    this.inFlightUpdates.set(updateId, connection);
    pendingUpdate.attempts++;
    pendingUpdate.status = 'sending';
    try {
      await connection.sendDocumentUpdate(pendingUpdate);
      if (this.pendingUpdates.get(updateId) === pendingUpdate) this.pendingUpdates.delete(updateId);
    } catch (error) {
      if (this.pendingUpdates.get(updateId) === pendingUpdate && this.inFlightUpdates.get(updateId) === connection) {
        pendingUpdate.status = pendingUpdate.attempts >= MAX_UPDATE_SEND_ATTEMPTS ? 'needsAttention' : 'queued';
      }
      if (this.connection === connection && pendingUpdate.status === 'needsAttention') {
        this.zone.run(() => {
          this.error.set(`Не удалось отправить правку после ${MAX_UPDATE_SEND_ATTEMPTS} попыток: ${error instanceof Error ? error.message : String(error)}`);
        });
      }
    } finally {
      if (this.inFlightUpdates.get(updateId) === connection) this.inFlightUpdates.delete(updateId);
    }
  }

  private subscribeToRemoteUpdates(document: Y.Doc, connection: ProjectHubClient, projectId: number, fileId: number): void {
    // Subscribe before starting, so updates arriving after JoinFile are observed.
    connection.onDocumentUpdate((updatedProjectId, updatedFileId, update) => {
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
  }

  private subscribeToConnectionClose(connection: ProjectHubClient): void {
    connection.onClose(error => {
      if (this.connection === connection) {
        this.zone.run(() => {
          this.stopPendingRetries();
          this.releaseInFlightUpdates(connection);
          this.connection = null;
          this.resetDocument();
          this.status.set('disconnected');
          if (error) this.error.set(error.message);
        });
      }
    });
  }

  private async startConnectionAndRestoreDocument(document: Y.Doc, connection: ProjectHubClient, projectId: number, fileId: number): Promise<void> {
    await connection.start();
    if (this.connection !== connection) return;
    await connection.joinFile(projectId, fileId);
    if (this.connection !== connection) return;
    const updates = await connection.getDocumentUpdates(projectId, fileId);
    if (this.connection !== connection) return;
    for (const update of updates) this.applyUpdate(document, update);
    const pendingForFile = [...this.pendingUpdates.values()].filter(
      update => update.projectId === projectId && update.fileId === fileId,
    );
    for (const update of pendingForFile) this.applyUpdate(document, update.base64);
    this.initialUpdateCount.set(updates.length); // An empty document returns [].
    this.status.set('connected');
    for (const update of pendingForFile) void this.sendPendingUpdate(connection, update);
    this.startPendingRetries(connection);
    console.info(`CodeRoom: received ${updates.length} existing updates for file ${fileId}.`);
  }

  private async handleConnectionFailure(connection: ProjectHubClient, error: unknown): Promise<void> {
    this.error.set(error instanceof Error ? error.message : String(error));
    this.status.set('disconnected');
    this.connection = null;
    this.resetDocument();
    await connection.stop();
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.stopPendingRetries();
    if (connection) this.releaseInFlightUpdates(connection);
    this.connection = null;
    this.resetDocument();
    this.status.set('disconnected');
    if (connection) await connection.stop();
  }

  private releaseInFlightUpdates(connection: ProjectHubClient): void {
    for (const pendingUpdate of this.pendingUpdates.values()) {
      if (this.inFlightUpdates.get(pendingUpdate.updateId) !== connection) continue;
      this.inFlightUpdates.delete(pendingUpdate.updateId);
      pendingUpdate.status = pendingUpdate.attempts >= MAX_UPDATE_SEND_ATTEMPTS ? 'needsAttention' : 'queued';
    }
  }

  private startPendingRetries(connection: ProjectHubClient): void {
    this.stopPendingRetries();
    this.zone.runOutsideAngular(() => {
      this.retryTimer = setInterval(() => {
        if (this.idleRetry !== null) return;
        const retry = () => {
          this.idleRetry = null;
          if (this.connection !== connection || this.status() !== 'connected') return;
          for (const update of this.pendingUpdates.values()) void this.sendPendingUpdate(connection, update);
        };
        if (typeof window.requestIdleCallback === 'function') {
          this.idleRetry = window.requestIdleCallback(retry, { timeout: PENDING_RETRY_INTERVAL_MS });
        } else {
          retry();
        }
      }, PENDING_RETRY_INTERVAL_MS);
    });
  }

  private stopPendingRetries(): void {
    if (this.retryTimer !== null) clearInterval(this.retryTimer);
    this.retryTimer = null;
    if (this.idleRetry !== null) window.cancelIdleCallback(this.idleRetry);
    this.idleRetry = null;
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
