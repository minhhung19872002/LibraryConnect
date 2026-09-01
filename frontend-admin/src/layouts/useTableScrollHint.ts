import { useEffect } from 'react';

/**
 * Đánh dấu mọi bảng đang còn cột nằm ngoài khung, để giao diện vẽ dấu hiệu cuộn ngang.
 *
 * Bảng của Ant Design cuộn ngang được ngay trong lòng nó, nhưng trên máy tính để bàn thì không có
 * thanh cuộn nào hiện ra: cán bộ chỉ thấy cột cuối bị cắt và không biết là còn cột nữa. Cột cuối
 * của hầu hết bảng quản trị lại đúng là cột **Thao tác** — chỗ đặt nút Sửa và Xoá — nên hậu quả
 * không phải thẩm mỹ mà là mất hẳn lối vào chức năng.
 *
 * Đo trên hệ thống đang chạy: 18 bảng của giao diện quản trị rơi vào tình trạng này, ví dụ Tiền phạt
 * rộng 1.500 px nằm trong khung 1.136 px, Yêu cầu đọc hạn chế 1.700 px trong 1.136 px.
 *
 * Làm ở tầng bố cục dùng chung chứ không sửa từng màn hình: 18 chỗ sửa tay thì vừa lâu vừa sót, mà
 * bảng thêm về sau lại không được che. Cách này che cả bảng chưa viết.
 */
export function useTableScrollHint(): void {
  useEffect(() => {
    const daGan = new WeakSet<HTMLElement>();
    let khung: number | null = null;

    const capNhat = (khungCuon: HTMLElement) => {
      const boc = khungCuon.closest('.ant-table-container') ?? khungCuon.parentElement;

      if (!boc) {
        return;
      }

      // Đo phần còn lại bên phải chứ không đo bề rộng tổng: cuộn tới hết bảng rồi thì không còn gì
      // để nhắc, mà dải mờ để nguyên lại che mất chính cột cuối cùng.
      const conCotBenPhai =
        khungCuon.scrollWidth - khungCuon.clientWidth - khungCuon.scrollLeft > 1;

      boc.classList.toggle('lc-bang--con-cot', conCotBenPhai);
    };

    const quet = () => {
      for (const khungCuon of document.querySelectorAll<HTMLElement>(
        '.ant-table-content, .ant-table-body',
      )) {
        capNhat(khungCuon);

        if (daGan.has(khungCuon)) {
          continue;
        }

        daGan.add(khungCuon);
        khungCuon.addEventListener('scroll', () => capNhat(khungCuon), { passive: true });
      }
    };

    const henQuet = () => {
      if (khung !== null) {
        return;
      }

      khung = window.requestAnimationFrame(() => {
        khung = null;
        quet();
      });
    };

    quet();

    // Bảng dựng lại mỗi lần đổi trang hay đổi bộ lọc, nên phải theo dõi cả cây tài liệu.
    const theoDoi = new MutationObserver(henQuet);
    theoDoi.observe(document.body, { childList: true, subtree: true });

    window.addEventListener('resize', henQuet);

    return () => {
      theoDoi.disconnect();
      window.removeEventListener('resize', henQuet);

      if (khung !== null) {
        window.cancelAnimationFrame(khung);
      }
    };
  }, []);
}
