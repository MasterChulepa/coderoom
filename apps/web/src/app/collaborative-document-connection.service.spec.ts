import { TestBed } from '@angular/core/testing';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import * as Y from 'yjs';
import { CollaborativeDocumentConnectionService } from './collaborative-document-connection.service';

describe('CollaborativeDocumentConnectionService', () => {
  it('restores remote updates without echoing them and sends a local text update', async () => {
    const source = new Y.Doc();
    const sourceText = source.getText('content');
    sourceText.insert(0, 'Hello');
    const initial = btoa(String.fromCharCode(...Y.encodeStateAsUpdate(source)));
    sourceText.insert(5, '!');
    const later = btoa(String.fromCharCode(...Y.encodeStateAsUpdate(source)));

    const handlers = new Map<string, (...args: unknown[]) => void>();
    const connection = {
      on: jasmine.createSpy('on').and.callFake((name: string, handler: (...args: unknown[]) => void) => {
        handlers.set(name, handler);
      }),
      onclose: jasmine.createSpy('onclose'),
      start: jasmine.createSpy('start').and.resolveTo(),
      invoke: jasmine.createSpy('invoke').and.callFake((method: string) =>
        Promise.resolve(method === 'GetCollaborativeDocumentUpdatesAsync' ? [initial] : undefined)),
      stop: jasmine.createSpy('stop').and.resolveTo(),
    };
    spyOn(HubConnectionBuilder.prototype, 'build').and.returnValue(connection as unknown as HubConnection);

    const service = TestBed.inject(CollaborativeDocumentConnectionService);
    await service.connect(1, 2);

    expect(connection.invoke.calls.allArgs()).toEqual([
      ['JoinFile', 1, 2],
      ['GetCollaborativeDocumentUpdatesAsync', 1, 2],
    ]);
    expect(service.text?.toString()).toBe('Hello');
    expect(service.initialUpdateCount()).toBe(1);

    const receive = handlers.get('DocumentUpdateReceived');
    expect(receive).toBeDefined();
    receive?.(1, 2, initial);
    receive?.(1, 2, later);

    expect(service.text?.toString()).toBe('Hello!');
    expect(service.documentContent()).toBe('Hello!');
    expect(service.receivedUpdateCount()).toBe(2);
    expect(connection.invoke).toHaveBeenCalledTimes(2);

    service.updateText('Hello!?');
    expect(service.documentContent()).toBe('Hello!?');
    expect(connection.invoke).toHaveBeenCalledTimes(3);

    const [method, projectId, fileId, updateId, payload] = connection.invoke.calls.mostRecent().args;
    expect(method).toBe('UpdateCollaborativeDocumentAsync');
    expect(projectId).toBe(1);
    expect(fileId).toBe(2);
    expect(updateId).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
    Y.applyUpdate(source, Uint8Array.from(atob(payload), char => char.charCodeAt(0)));
    expect(sourceText.toString()).toBe('Hello!?');

    await service.disconnect();
    source.destroy();
  });
});
