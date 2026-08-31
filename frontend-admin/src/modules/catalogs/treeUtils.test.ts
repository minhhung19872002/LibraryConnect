import { describe, expect, it } from 'vitest';
import { buildTreeSelectData, flattenTree } from './treeUtils';
import type { CatalogTreeNode } from './types';

function node(id: string, code: string, name: string, children: CatalogTreeNode[] = []): CatalogTreeNode {
  return { id, code, name, isActive: true, children };
}

const ddc: CatalogTreeNode[] = [
  node('500', '500', 'Khoa học tự nhiên', [
    node('510', '510', 'Toán học'),
    node('530', '530', 'Vật lý học', [node('531', '531', 'Cơ học')]),
  ]),
  node('600', '600', 'Công nghệ'),
];

describe('buildTreeSelectData', () => {
  it('keeps the tree shape and labels each node with its code', () => {
    const data = buildTreeSelectData(ddc);

    expect(data).toHaveLength(2);
    expect(data[0]?.title).toBe('500 — Khoa học tự nhiên');
    expect(data[0]?.children).toHaveLength(2);
    expect(data[0]?.children?.[1]?.children?.[0]?.title).toBe('531 — Cơ học');
  });

  it('marks an inactive node as disabled rather than hiding it', () => {
    // A value that is no longer in use still has to be visible while browsing existing records.
    const withInactive = [{ ...node('700', '700', 'Nghệ thuật'), isActive: false }];

    expect(buildTreeSelectData(withInactive)[0]?.disabled).toBe(true);
  });

  it('excludes a node together with its whole branch', () => {
    // The edit form uses this so a value cannot be selected as its own parent, which would detach
    // the branch from the tree.
    const data = buildTreeSelectData(ddc, '530');

    expect(data[0]?.children).toHaveLength(1);
    expect(data[0]?.children?.[0]?.value).toBe('510');
  });

  it('leaves a node without children with no children array', () => {
    expect(buildTreeSelectData(ddc)[1]?.children).toBeUndefined();
  });

  it('falls back to the name when a catalogue has no codes', () => {
    const keywords = [node('1', '', 'Cơ sở dữ liệu')];

    expect(buildTreeSelectData(keywords)[0]?.title).toBe('Cơ sở dữ liệu');
  });
});

describe('flattenTree', () => {
  it('returns every node once, depth first', () => {
    expect(flattenTree(ddc).map((item) => item.code)).toEqual(['500', '510', '530', '531', '600']);
  });

  it('returns nothing for an empty tree', () => {
    expect(flattenTree([])).toEqual([]);
  });
});
