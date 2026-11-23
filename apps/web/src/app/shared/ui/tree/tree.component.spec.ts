import { TestBed } from '@angular/core/testing';
import { TreeComponent } from './tree.component';

describe('TreeComponent', () => {
  it('navigates folders without activating a file until Enter is pressed', () => {
    const fixture = TestBed.createComponent(TreeComponent);
    fixture.componentRef.setInput('label', 'Files');
    fixture.componentRef.setInput('nodes', [
      { id: 'src', label: 'src', children: [{ id: 1, label: 'main.ts' }] },
      { id: 2, label: 'README.md' },
    ]);
    const activate = jasmine.createSpy('activate');
    fixture.componentInstance.nodeActivated.subscribe(activate);
    fixture.detectChanges();

    const tree: HTMLElement = fixture.nativeElement;
    const items = () => tree.querySelectorAll<HTMLElement>('[role="treeitem"]');
    const press = (item: HTMLElement, key: string) => {
      item.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
      fixture.detectChanges();
    };

    const folder = items()[0];
    expect(items().length).toBe(2);
    folder.focus();
    press(folder, 'ArrowRight');
    expect(folder.getAttribute('aria-expanded')).toBe('true');
    expect(items().length).toBe(3);

    const file = items()[1];
    press(folder, 'ArrowRight');
    expect(document.activeElement).toBe(file);
    expect(activate).not.toHaveBeenCalled();
    press(file, 'Enter');
    expect(activate).toHaveBeenCalledOnceWith(1);

    press(file, 'ArrowLeft');
    expect(document.activeElement).toBe(folder);
    press(folder, 'ArrowLeft');
    expect(items().length).toBe(2);
  });
});
