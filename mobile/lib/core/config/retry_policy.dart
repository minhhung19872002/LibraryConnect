/// Chính sách thử lại của Riverpod cho toàn ứng dụng.
///
/// Riverpod 3 mặc định tự chạy lại một provider lỗi tới 10 lần, giãn cách 200 ms → 6,4 s, và
/// trong lúc chờ thử lại `AsyncValue.when` vẫn hiện nhánh **đang tải**. Với ứng dụng này mọi lỗi
/// đáng gặp đều là [ApiException] (mất mạng, 403, 404) — thử lại tự động không đổi được kết
/// quả, chỉ biến "Không kết nối được" thành một vòng quay gần 40 giây trước khi báo lỗi, trái với
/// đặc tả mục 5 ("không được màn hình trắng hoặc quay vòng vô tận"). Mỗi màn hình đã có nút Thử
/// lại của riêng nó và các danh sách quan trọng rơi về bản đệm, nên tắt hẳn: lỗi hiện ngay.
///
/// Dùng ở `main.dart` và trong mọi `ProviderScope` của phép thử widget, để phép thử thấy đúng
/// thứ người dùng thấy.
Duration? lcRetry(int retryCount, Object error) => null;
