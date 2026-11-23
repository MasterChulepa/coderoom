import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
import { CollaborativeDocumentConnectionService } from './workspace/collaboration/collaborative-document-connection.service';

describe('Workspace', () => {
  it('loads the project tree and opens the latest requested file', async () => {
    let finishFirstOpen!: () => void;
    const firstOpen = new Promise<void>(resolve => { finishFirstOpen = resolve; });
    const connection = {
      status: signal('disconnected'),
      error: signal(null),
      text: null,
      connect: jasmine.createSpy('connect').and.callFake(async (_projectId: number, fileId: number) => {
        if (fileId === 10) await firstOpen;
        connection.status.set('connected');
      }),
      disconnect: jasmine.createSpy('disconnect').and.resolveTo(),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: CollaborativeDocumentConnectionService, useValue: connection },
      ],
    });

    const harness = await RouterTestingHarness.create('/projects/42');
    const http = TestBed.inject(HttpTestingController);
    expect(harness.routeNativeElement?.textContent).toContain('Загрузка файлов');
    http.expectOne('/api/projects/42/files').flush([
      { id: 10, path: 'src/a.ts' },
      { id: 20, path: 'src/b.ts' },
      { id: 30, path: 'src/c.ts' },
    ]);
    harness.detectChanges();
    const items = () => harness.routeNativeElement!.querySelectorAll<HTMLElement>('[role="treeitem"]');
    expect(items().length).toBe(1);
    items()[0].click();
    harness.detectChanges();
    expect(items().length).toBe(4);

    items()[1].click();
    items()[2].click();
    items()[3].click();
    harness.detectChanges();
    expect(connection.connect.calls.allArgs()).toEqual([[42, 10]]);
    expect(harness.routeNativeElement?.textContent).toContain('Загрузка документа');

    finishFirstOpen();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(connection.connect.calls.allArgs()).toEqual([[42, 10], [42, 30]]);
    expect(items()[3].getAttribute('aria-selected')).toBe('true');
    expect(harness.routeNativeElement?.querySelector('.editor-header')?.textContent).toContain('src/c.ts');
    http.verify();
  });
});
