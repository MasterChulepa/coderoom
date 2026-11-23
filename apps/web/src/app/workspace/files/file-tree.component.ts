import { Component, computed, input, output } from '@angular/core';
import { TreeComponent, type TreeNode, type TreeNodeId } from '../../shared/ui/tree';

export interface FileTreeEntry {
  readonly id: number;
  readonly path: string;
}

@Component({
  selector: 'app-file-tree',
  imports: [TreeComponent],
  template: `
    <app-tree label="Файлы проекта" [nodes]="nodes()" [selectedId]="selectedId()"
      (nodeActivated)="openFile($event)" />
  `,
})
export class FileTreeComponent {
  readonly files = input.required<readonly FileTreeEntry[]>();
  readonly selectedId = input<number | null>(null);
  readonly fileSelected = output<number>();

  protected readonly nodes = computed(() => buildFileTree(this.files()));

  protected openFile(id: TreeNodeId): void {
    if (typeof id === 'number') this.fileSelected.emit(id);
  }
}

function buildFileTree(files: readonly FileTreeEntry[]): TreeNode[] {
  const root: TreeNode[] = [];
  const folders = new Map<string, TreeNode[]>();
  for (const file of files) {
    const segments = file.path.split('/').filter(Boolean);
    const label = segments.pop() ?? file.path;
    let children = root;
    let folderPath = '';
    for (const segment of segments) {
      folderPath += `/${segment}`;
      let folderChildren = folders.get(folderPath);
      if (!folderChildren) {
        folderChildren = [];
        folders.set(folderPath, folderChildren);
        children.push({ id: folderPath, label: segment, children: folderChildren });
      }
      children = folderChildren;
    }
    children.push({ id: file.id, label });
  }
  for (const siblings of [root, ...folders.values()]) {
    siblings.sort((a, b) =>
      Number(Boolean(b.children)) - Number(Boolean(a.children)) || a.label.localeCompare(b.label),
    );
  }
  return root;
}
