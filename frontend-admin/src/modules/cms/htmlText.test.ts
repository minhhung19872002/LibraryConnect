import { describe, expect, it } from 'vitest';
import { escapeHtml } from './htmlText';

describe('Chữ đưa vào HTML của trình soạn thảo', () => {
  it('thoát năm ký tự đặc biệt để tên tệp không vỡ thẻ', () => {
    expect(escapeHtml('Bao cao <Q3> & "ket qua" \'2026\'.pdf')).toBe(
      'Bao cao &lt;Q3&gt; &amp; &quot;ket qua&quot; &#39;2026&#39;.pdf',
    );
  });

  it('giữ nguyên tiếng Việt có dấu', () => {
    expect(escapeHtml('Quyết định số 12.docx')).toBe('Quyết định số 12.docx');
  });
});
