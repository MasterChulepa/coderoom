import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  signal,
  viewChildren,
} from '@angular/core';
import { buttonVariants } from '@spartan-ng/helm/button';
import { hlm } from '@spartan-ng/helm/utils';
import type { TreeNode, TreeNodeId } from './tree-node';

interface TreeRow {
  node: TreeNode;
  parentId: TreeNodeId | null;
  level: number;
  position: number;
  siblingCount: number;
  hasChildren: boolean;
  expanded: boolean;
}

@Component({
  selector: 'app-tree',
  templateUrl: './tree.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TreeComponent {
  readonly nodes = input.required<readonly TreeNode[]>();
  readonly label = input.required<string>();
  readonly selectedId = input<TreeNodeId | null>(null);
  readonly nodeActivated = output<TreeNodeId>();

  protected readonly rowClass = hlm(
    buttonVariants({ variant: 'ghost', size: 'sm' }),
    'flex h-auto min-h-8 w-full cursor-pointer justify-start whitespace-normal py-1 text-start font-normal',
    'aria-selected:bg-accent aria-selected:text-accent-foreground focus-visible:ring-inset',
  );

  private readonly expandedIds = signal<ReadonlySet<TreeNodeId>>(new Set());
  private readonly focusedId = signal<TreeNodeId | null>(null);
  private readonly items = viewChildren<ElementRef<HTMLLIElement>>('item');

  protected readonly rows = computed(() => {
    const rows: TreeRow[] = [];
    const expandedIds = this.expandedIds();
    const visit = (nodes: readonly TreeNode[], parentId: TreeNodeId | null, level: number): void => {
      nodes.forEach((node, index) => {
        const children = node.children ?? [];
        const hasChildren = children.length > 0;
        const expanded = hasChildren && expandedIds.has(node.id);
        rows.push({ node, parentId, level, position: index + 1, siblingCount: nodes.length, hasChildren, expanded });
        if (expanded) visit(children, node.id, level + 1);
      });
    };
    visit(this.nodes(), null, 1);
    return rows;
  });

  protected readonly tabStopId = computed(() => {
    const rows = this.rows();
    return rows.find(row => row.node.id === this.focusedId())?.node.id
      ?? rows.find(row => row.node.id === this.selectedId())?.node.id
      ?? rows[0]?.node.id;
  });

  protected rememberFocus(id: TreeNodeId): void {
    this.focusedId.set(id);
  }

  protected activate(row: TreeRow, index: number): void {
    this.focusRow(index);
    if (row.hasChildren) {
      this.setExpanded(row.node.id, !row.expanded);
    } else {
      this.nodeActivated.emit(row.node.id);
    }
  }

  protected handleKey(event: KeyboardEvent, row: TreeRow, index: number): void {
    if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;

    switch (event.key) {
      case 'ArrowDown':
        this.focusRow(index + 1);
        break;
      case 'ArrowUp':
        this.focusRow(index - 1);
        break;
      case 'Home':
        this.focusRow(0);
        break;
      case 'End':
        this.focusRow(this.rows().length - 1);
        break;
      case 'ArrowRight':
        if (row.expanded) {
          this.focusRow(index + 1);
        } else if (row.hasChildren) {
          this.setExpanded(row.node.id, true);
        }
        break;
      case 'ArrowLeft':
        if (row.expanded) {
          this.setExpanded(row.node.id, false);
        } else if (row.parentId !== null) {
          this.focusRow(this.rows().findIndex(item => item.node.id === row.parentId));
        }
        break;
      case 'Enter':
      case ' ':
        this.activate(row, index);
        break;
      default:
        return;
    }
    event.preventDefault();
  }

  private focusRow(index: number): void {
    this.items()[index]?.nativeElement.focus();
  }

  private setExpanded(id: TreeNodeId, expanded: boolean): void {
    this.expandedIds.update(ids => {
      const next = new Set(ids);
      if (expanded) next.add(id);
      else next.delete(id);
      return next;
    });
  }
}
