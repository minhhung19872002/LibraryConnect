import { describe, expect, it } from 'vitest';
import { MENU_ICONS, dropModeOf, moveMenu, toPositions } from './menuTree';
import type { CmsMenu } from './types';

function menu(id: string, children: CmsMenu[] = []): CmsMenu {
  return { id, name: id, url: `/${id}`, sortOrder: 0, isActive: true, children };
}

const tree = [menu('home'), menu('about', [menu('rules'), menu('faq')]), menu('contact')];

describe('Kéo thả cây menu', () => {
  it('suy ra vị trí thả từ thông tin của Ant Design', () => {
    expect(dropModeOf(false, 1, '0-1')).toBe('inside');
    expect(dropModeOf(true, 0, '0-1')).toBe('before');
    expect(dropModeOf(true, 2, '0-1')).toBe('after');
  });

  it('thả vào khe sau một mục cùng cấp thì đổi thứ tự, không đổi cấp', () => {
    const result = moveMenu(tree, 'home', 'contact', 'after')!;

    expect(result.map((item) => item.id)).toEqual(['about', 'contact', 'home']);
    expect(result.every((item) => item.id !== 'home' || item.children.length === 0)).toBe(true);
  });

  it('thả vào giữa một mục thì thành con đầu tiên của mục ấy', () => {
    const result = moveMenu(tree, 'contact', 'about', 'inside')!;
    const about = result.find((item) => item.id === 'about')!;

    expect(result.map((item) => item.id)).toEqual(['home', 'about']);
    expect(about.children.map((item) => item.id)).toEqual(['contact', 'rules', 'faq']);
  });

  it('kéo mục con ra khe trên cùng thì nó lên cấp cao nhất', () => {
    const result = moveMenu(tree, 'faq', 'home', 'before')!;
    const about = result.find((item) => item.id === 'about')!;

    expect(result.map((item) => item.id)).toEqual(['faq', 'home', 'about', 'contact']);
    expect(about.children.map((item) => item.id)).toEqual(['rules']);
  });

  it('không cho thả mục cha vào trong mục con của chính nó — cây sẽ thành vòng', () => {
    expect(moveMenu(tree, 'about', 'rules', 'inside')).toBeNull();
    expect(moveMenu(tree, 'about', 'faq', 'after')).toBeNull();
    expect(moveMenu(tree, 'about', 'about', 'after')).toBeNull();
  });

  it('trải cây thành vị trí phẳng với mã cha và thứ tự cách nhau mười', () => {
    const positions = toPositions(tree);

    expect(positions).toEqual([
      { id: 'home', parentId: undefined, sortOrder: 10 },
      { id: 'about', parentId: undefined, sortOrder: 20 },
      { id: 'rules', parentId: 'about', sortOrder: 10 },
      { id: 'faq', parentId: 'about', sortOrder: 20 },
      { id: 'contact', parentId: undefined, sortOrder: 30 },
    ]);
  });

  it('danh sách biểu tượng có nhãn tiếng Việt và tên không trùng', () => {
    expect(MENU_ICONS.length).toBeGreaterThan(5);
    expect(new Set(MENU_ICONS.map((icon) => icon.value)).size).toBe(MENU_ICONS.length);
    MENU_ICONS.forEach((icon) => expect(icon.label).toBeTruthy());
  });
});
