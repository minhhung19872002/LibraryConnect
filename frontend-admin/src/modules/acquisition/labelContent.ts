import { money } from './labels';
import type { StockItemDto } from './types';

/**
 * Nội dung một ô tem — bản chiếu của `LabelContentBuilder` ở máy chủ.
 *
 * Ô xem trước trên trình duyệt phải hiện đúng cái máy in sẽ in. Hai bộ mã ở hai ngôn ngữ nên không
 * dùng chung được; thay vào đó phép thử của bên này lặp lại đúng các trường hợp của bên kia.
 */

export interface LabelData {
  barcode: string;
  registerNumber: string;
  callNumber?: string | null;
  ddc?: string | null;
  title: string;
  author?: string | null;
  libraryName?: string | null;
  warehouseName?: string | null;
  isbn?: string | null;
  publishYear?: number | null;
  price: number;
  copyNumber: number;
}

/** Dữ liệu tem từ một dòng của danh sách kho — tên thư viện và DDC không có ở danh sách nên để trống. */
export function toLabelData(item: StockItemDto, libraryName?: string | null): LabelData {
  return {
    barcode: item.barcode,
    registerNumber: item.registerNumber,
    callNumber: item.callNumber,
    ddc: null,
    title: item.title,
    author: item.authorMain,
    libraryName: libraryName ?? null,
    warehouseName: item.warehouseName,
    isbn: item.isbn,
    publishYear: null,
    price: item.price,
    copyNumber: item.copyNumber,
  };
}

function callNumberLine(callNumber: string | null | undefined, index: number): string {
  if (!callNumber || !callNumber.trim()) return '';

  const parts = callNumber.split(' ').map((part) => part.trim()).filter(Boolean);

  // Ký hiệu dài hơn ba thành phần thì phần thừa dồn vào dòng cuối, không rơi mất.
  if (index === 2 && parts.length > 3) return parts.slice(2).join(' ');

  return index < parts.length ? parts[index]! : '';
}

/** Chuỗi in ra của một ô theo nguồn nội dung. */
export function resolveLabelText(data: LabelData, source: string): string {
  const key = source.trim();

  if (!key) return '';
  if (key.startsWith('"')) return key.replace(/^"+|"+$/g, '');

  switch (key) {
    case 'barcode':
      return data.barcode;
    case 'registerNumber':
      return data.registerNumber;
    case 'callNumber':
      return data.callNumber ?? '';
    case 'ddc':
      return data.ddc ?? '';
    case 'title':
      return data.title;
    case 'author':
      return data.author ?? '';
    case 'libraryName':
      return data.libraryName ?? '';
    case 'warehouseName':
      return data.warehouseName ?? '';
    case 'isbn':
      return data.isbn ?? '';
    case 'publishYear':
      return data.publishYear ? String(data.publishYear) : '';
    case 'price':
      return money(data.price);
    case 'copyNumber':
      return String(data.copyNumber);
    case 'callNumberLine1':
      return callNumberLine(data.callNumber, 0);
    case 'callNumberLine2':
      return callNumberLine(data.callNumber, 1);
    case 'callNumberLine3':
      return callNumberLine(data.callNumber, 2);
    default:
      return '';
  }
}

/** Giá trị được mã hóa thành vạch. */
export function resolveBarcodeValue(data: LabelData, source: string): string {
  switch (source) {
    case 'registerNumber':
      return data.registerNumber;
    case 'callNumber':
      return data.callNumber ?? data.barcode;
    default:
      return data.barcode;
  }
}

/** Dữ liệu mẫu cho ô xem trước của trình thiết kế, khi chưa chọn ấn phẩm thật nào. */
export const sampleLabelData: LabelData = {
  barcode: 'LC00000123',
  registerNumber: 'ĐKCB00000123',
  callNumber: '005.74 NGU 1',
  ddc: '005.74',
  title: 'Giáo trình cơ sở dữ liệu',
  author: 'Nguyễn Văn An',
  libraryName: 'Thư viện',
  warehouseName: 'Kho mở',
  isbn: '9786040123456',
  publishYear: 2024,
  price: 150000,
  copyNumber: 1,
};
