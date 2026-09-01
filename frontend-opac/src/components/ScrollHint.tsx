import { useEffect, useRef, useState, type DependencyList, type ReactNode } from 'react';
import { Typography } from 'antd';

/**
 * Dòng nhắc cuộn ngang cho những bảng rộng hơn khung chứa.
 *
 * Bảng của Ant Design cuộn ngang được ngay bên trong bảng, nhưng trên máy tính để bàn thì không có
 * thanh cuộn nào hiện ra: người xem chỉ thấy cột cuối bị cắt mất một nửa và kết luận là dữ liệu bị
 * cụt. Đo thật xem bảng có tràn khung không rồi mới nhắc — nhắc lúc nào cũng nhắc thì trên màn hình
 * rộng nó thành câu thừa.
 */
export function ScrollHint({
  children,
  deps = [],
  message = 'Còn cột bên phải — cuộn ngang trong bảng để xem hết.',
}: {
  children: ReactNode;
  deps?: DependencyList;
  message?: string;
}) {
  const khung = useRef<HTMLDivElement>(null);
  const [tran, setTran] = useState(false);

  useEffect(() => {
    const bang = khung.current?.querySelector<HTMLElement>('.ant-table-body')
      ?? khung.current?.querySelector<HTMLElement>('.ant-table-content');

    if (!bang) {
      return undefined;
    }

    // Đo phần còn lại bên phải chứ không đo bề rộng tổng: cuộn tới hết bảng rồi thì không còn
    // gì để nhắc nữa, mà dải bóng mờ để nguyên lại che mất chính cột cuối cùng.
    const do_ = () =>
      setTran(bang.scrollWidth - bang.clientWidth - bang.scrollLeft > 1);

    do_();

    const quanSat = new ResizeObserver(do_);
    quanSat.observe(bang);

    bang.addEventListener('scroll', do_);

    return () => {
      quanSat.disconnect();
      bang.removeEventListener('scroll', do_);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return (
    <div ref={khung} className={tran ? 'lc-scroll-hint lc-scroll-hint--tran' : 'lc-scroll-hint'}>
      {tran ? (
        <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 8, fontSize: 13 }}>
          {message} →
        </Typography.Text>
      ) : null}
      {children}
    </div>
  );
}
