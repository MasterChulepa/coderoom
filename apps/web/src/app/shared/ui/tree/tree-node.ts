export type TreeNodeId = string | number;

export interface TreeNode {
  /** Stable and unique within the tree, including across branches. */
  readonly id: TreeNodeId;
  readonly label: string;
  readonly children?: readonly TreeNode[];
}
