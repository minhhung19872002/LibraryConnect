import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { ApiRequestError } from '@/api/client';

/**
 * Máy chủ còn trả lời hay không.
 *
 * Không phải huy hiệu trang trí. Giao diện quản trị là một trang đơn chạy sau Nginx: API chết thì
 * khung trang vẫn hiện nguyên vẹn từ bộ nhớ của trình duyệt, menu vẫn bấm được, biểu mẫu vẫn gõ
 * được — chỉ có nút Lưu là im lặng. Cán bộ ngồi bấm mãi rồi kết luận "phần mềm treo".
 *
 * Cách đo: nghe kho lượt gọi của react-query, chứ **không** gọi thêm một lượt kiểm tra sức khoẻ
 * nào của riêng mình. Thêm một lượt gọi mỗi mười giây cho hai trăm người dùng đồng thời là hai
 * chục lượt mỗi giây chỉ để vẽ một chấm tròn, mà lượt ấy hỏng hay không cũng chưa chắc nói đúng
 * chuyện những lượt gọi thật đang gặp.
 *
 * Chỉ lỗi mạng — `status === 0`, tức không nối được tới máy chủ — mới tính là mất kết nối. Lỗi 403
 * hay 404 nghĩa là máy chủ vẫn sống và vừa trả lời đàng hoàng; báo "mất kết nối" trong hai trường
 * hợp ấy là nói sai, và cán bộ sẽ thôi tin cái huy hiệu này ngay lần đầu.
 */
export function useServerStatus(): boolean {
  const queryClient = useQueryClient();
  const [connected, setConnected] = useState(true);

  useEffect(() => {
    const cache = queryClient.getQueryCache();

    const doLai = () => {
      // So theo **thời điểm**, không theo số lượng: chuyện gần nhất mới là chuyện đang đúng. Một
      // lượt gọi hỏng từ mười phút trước không nói được gì về lúc này, và ngược lại, một lượt
      // thành công cũ cũng không cứu được cho hiện tại.
      let lanCuoiThanhCong = 0;
      let lanCuoiMatMang = 0;

      for (const query of cache.getAll()) {
        const { dataUpdatedAt, error, errorUpdatedAt } = query.state;

        if (dataUpdatedAt > lanCuoiThanhCong) {
          lanCuoiThanhCong = dataUpdatedAt;
        }

        if (
          error instanceof ApiRequestError &&
          error.status === 0 &&
          errorUpdatedAt > lanCuoiMatMang
        ) {
          lanCuoiMatMang = errorUpdatedAt;
        }
      }

      setConnected(lanCuoiMatMang === 0 || lanCuoiThanhCong >= lanCuoiMatMang);
    };

    doLai();

    return cache.subscribe(doLai);
  }, [queryClient]);

  return connected;
}
