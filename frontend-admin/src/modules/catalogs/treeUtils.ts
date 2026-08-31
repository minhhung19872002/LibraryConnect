import type { CatalogTreeNode } from './types';

export interface TreeSelectNode {
  value: string;
  title: string;
  children?: TreeSelectNode[];
  disabled?: boolean;
}

/**
 * Converts the catalogue tree into the shape Ant Design's TreeSelect expects.
 *
 * `excludeId` removes a node and its whole branch, which is what stops the edit form from offering
 * a value as its own parent — a choice the backend would reject anyway.
 */
export function buildTreeSelectData(nodes: CatalogTreeNode[], excludeId?: string): TreeSelectNode[] {
  return nodes
    .filter((node) => node.id !== excludeId)
    .map((node) => ({
      value: node.id,
      title: node.code ? `${node.code} — ${node.name}` : node.name,
      disabled: !node.isActive,
      children: node.children.length > 0 ? buildTreeSelectData(node.children, excludeId) : undefined,
    }));
}

/** Flattens the tree, used where a plain list of every node is easier to work with. */
export function flattenTree(nodes: CatalogTreeNode[]): CatalogTreeNode[] {
  return nodes.flatMap((node) => [node, ...flattenTree(node.children)]);
}
