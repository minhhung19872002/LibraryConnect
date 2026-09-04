/**
 * Chia sẻ một trang tài liệu (IX.2): sao chép liên kết, hộp chia sẻ của hệ điều hành khi trình
 * duyệt có (điện thoại), và hai mạng bạn đọc Việt Nam dùng nhiều nhất — Facebook và Zalo.
 */

export interface ShareTargets {
  facebook: string;
  zalo: string;
}

/** Địa chỉ mở hộp chia sẻ của từng mạng; liên kết và nhan đề được mã hóa để không vỡ khi có dấu hay `&`. */
export function shareTargets(url: string, title: string): ShareTargets {
  const link = encodeURIComponent(url);
  const text = encodeURIComponent(title);

  return {
    facebook: `https://www.facebook.com/sharer/sharer.php?u=${link}`,
    zalo: `https://sp.zalo.me/share?url=${link}&title=${text}`,
  };
}

/** Trình duyệt có hộp chia sẻ của hệ điều hành hay không (Web Share API). */
export function canNativeShare(nav: Pick<Navigator, 'share'> | undefined = globalThis.navigator): boolean {
  return typeof nav?.share === 'function';
}

/**
 * Sao chép chữ vào bộ nhớ tạm. Trả về false khi trình duyệt không cho (trang không phải HTTPS,
 * hay người dùng từ chối) để giao diện hiện liên kết cho họ tự chép, chứ không báo "đã sao chép" bừa.
 */
export async function copyText(
  text: string,
  clipboard: Pick<Clipboard, 'writeText'> | undefined = globalThis.navigator?.clipboard,
): Promise<boolean> {
  if (!clipboard) return false;

  try {
    await clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}
