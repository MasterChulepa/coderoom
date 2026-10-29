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

  it('keeps an unconfirmed update on disconnect and retries it with the same ID', async () => {
    let rejectFirstSend!: (reason: Error) => void;
    const firstSend = new Promise<void>((_, reject) => { rejectFirstSend = reject; });
    const createConnection = (send: () => Promise<void>) => ({
      on: jasmine.createSpy('on'),
      onclose: jasmine.createSpy('onclose'),
      start: jasmine.createSpy('start').and.resolveTo(),
      invoke: jasmine.createSpy('invoke').and.callFake((method: string) => {
        if (method === 'GetCollaborativeDocumentUpdatesAsync') return Promise.resolve([]);
        if (method === 'UpdateCollaborativeDocumentAsync') return send();
        return Promise.resolve();
      }),
      stop: jasmine.createSpy('stop').and.resolveTo(),
    });
    const firstConnection = createConnection(() => firstSend);
    const secondConnection = createConnection(() => Promise.resolve());
    spyOn(HubConnectionBuilder.prototype, 'build').and.returnValues(
      firstConnection as unknown as HubConnection,
      secondConnection as unknown as HubConnection,
    );

    const service = TestBed.inject(CollaborativeDocumentConnectionService);
    await service.connect(1, 2);
    service.updateText('draft');

    const [pending] = [...service.pendingUpdates.values()];
    expect(pending).toEqual(jasmine.objectContaining({ projectId: 1, fileId: 2 }));
    expect(pending.attempts).toBe(1);
    expect(pending.status).toBe('sending');
    expect(firstConnection.invoke.calls.mostRecent().args).toEqual([
      'UpdateCollaborativeDocumentAsync', 1, 2, pending.updateId, pending.base64,
    ]);

    await service.disconnect();
    expect(service.pendingUpdates.get(pending.updateId)).toBe(pending);
    expect(pending.status).toBe('queued');
    rejectFirstSend(new Error('connection lost'));
    await Promise.resolve();
    expect(service.pendingUpdates.get(pending.updateId)).toBe(pending);

    await service.connect(1, 2);
    expect(service.documentContent()).toBe('draft');
    expect(secondConnection.invoke.calls.mostRecent().args).toEqual([
      'UpdateCollaborativeDocumentAsync', 1, 2, pending.updateId, pending.base64,
    ]);
    expect(pending.attempts).toBe(2);
    await Promise.resolve();
    expect(service.pendingUpdates.size).toBe(0);

    await service.disconnect();
  });

  it('retries updates for an inactive file during idle time and stops scheduling on disconnect', async () => {
    let tick!: () => void;
    let idle!: IdleRequestCallback;
    spyOn(window, 'setInterval').and.callFake(((callback: () => void) => {
      tick = callback;
      return 123;
    }) as typeof window.setInterval);
    const clearIntervalSpy = spyOn(window, 'clearInterval');
    spyOn(window, 'requestIdleCallback').and.callFake((callback: IdleRequestCallback) => {
      idle = callback;
      return 456;
    });
    const connection = {
      on: jasmine.createSpy('on'),
      onclose: jasmine.createSpy('onclose'),
      start: jasmine.createSpy('start').and.resolveTo(),
      invoke: jasmine.createSpy('invoke').and.callFake((method: string) =>
        Promise.resolve(method === 'GetCollaborativeDocumentUpdatesAsync' ? [] : undefined)),
      stop: jasmine.createSpy('stop').and.resolveTo(),
    };
    spyOn(HubConnectionBuilder.prototype, 'build').and.returnValue(connection as unknown as HubConnection);

    const service = TestBed.inject(CollaborativeDocumentConnectionService);
    const pending = { projectId: 1, fileId: 2, updateId: 'update-id', base64: 'payload', attempts: 0,
      status: 'queued' as const };
    service.pendingUpdates.set(pending.updateId, pending);
    await service.connect(1, 3);
    tick();
    expect(connection.invoke).toHaveBeenCalledTimes(2);
    idle({ didTimeout: false, timeRemaining: () => 10 });
    expect(connection.invoke.calls.mostRecent().args).toEqual(['UpdateCollaborativeDocumentAsync', 1, 2, 'update-id', 'payload']);
    await Promise.resolve();
    expect(service.pendingUpdates.size).toBe(0);

    await service.disconnect();
    expect(clearIntervalSpy).toHaveBeenCalledWith(123);
  });

  it('stops retrying an update after three failed sends', async () => {
    let tick!: () => void;
    let idle!: IdleRequestCallback;
    spyOn(window, 'setInterval').and.callFake(((callback: () => void) => {
      tick = callback;
      return 123;
    }) as typeof window.setInterval);
    spyOn(window, 'requestIdleCallback').and.callFake((callback: IdleRequestCallback) => {
      idle = callback;
      return 456;
    });
    const connection = {
      on: jasmine.createSpy('on'),
      onclose: jasmine.createSpy('onclose'),
      start: jasmine.createSpy('start').and.resolveTo(),
      invoke: jasmine.createSpy('invoke').and.callFake((method: string) => {
        if (method === 'GetCollaborativeDocumentUpdatesAsync') return Promise.resolve([]);
        if (method === 'UpdateCollaborativeDocumentAsync') return Promise.reject(new Error('offline'));
        return Promise.resolve();
      }),
      stop: jasmine.createSpy('stop').and.resolveTo(),
    };
    spyOn(HubConnectionBuilder.prototype, 'build').and.returnValue(connection as unknown as HubConnection);

    const service = TestBed.inject(CollaborativeDocumentConnectionService);
    await service.connect(1, 2);
    service.updateText('draft');
    const [pending] = [...service.pendingUpdates.values()];
    await Promise.resolve();
    expect(pending.attempts).toBe(1);
    expect(pending.status).toBe('queued');

    for (let attempt = 2; attempt <= 3; attempt++) {
      tick();
      idle({ didTimeout: false, timeRemaining: () => 10 });
      await Promise.resolve();
      expect(pending.attempts).toBe(attempt);
    }

    expect(pending.status).toBe('needsAttention');
    expect(service.error()).toContain('после 3 попыток');
    tick();
    idle({ didTimeout: false, timeRemaining: () => 10 });
    expect(connection.invoke).toHaveBeenCalledTimes(5); // JoinFile, history, three sends.
    expect(service.pendingUpdates.get(pending.updateId)).toBe(pending);

    await service.disconnect();
  });
});
