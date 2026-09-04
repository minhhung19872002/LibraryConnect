/**
 * Chữ thường đưa vào HTML do trình soạn thảo sinh ra — tên tệp đính kèm chẳng hạn. Tên tệp là do
 * người dùng đặt, có thể chứa `<` hay `&`; đưa thẳng vào bài là vỡ thẻ hoặc tệ hơn.
 */
export function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
