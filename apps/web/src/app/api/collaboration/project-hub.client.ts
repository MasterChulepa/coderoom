import { HubConnectionBuilder } from '@microsoft/signalr';
import type { DocumentUpdatePayload } from './collaboration.dto';

export class ProjectHubClient {
  private readonly connection = new HubConnectionBuilder().withUrl('/hubs/projects').build();

  start(): Promise<void> {
    return this.connection.start();
  }

  stop(): Promise<void> {
    return this.connection.stop();
  }

  joinFile(projectId: number, fileId: number): Promise<void> {
    return this.connection.invoke('JoinFile', projectId, fileId);
  }

  getDocumentUpdates(projectId: number, fileId: number): Promise<string[]> {
    return this.connection.invoke('GetCollaborativeDocumentUpdatesAsync', projectId, fileId);
  }

  sendDocumentUpdate({ projectId, fileId, updateId, base64 }: DocumentUpdatePayload): Promise<void> {
    return this.connection.invoke('UpdateCollaborativeDocumentAsync', projectId, fileId, updateId, base64);
  }

  onDocumentUpdate(handler: (projectId: number, fileId: number, base64: string) => void): void {
    this.connection.on('DocumentUpdateReceived', handler);
  }

  onClose(handler: (error?: Error) => void): void {
    this.connection.onclose(handler);
  }
}
