package vn.bluestar.libraryconnect_mobile

import android.view.WindowManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

/**
 * Kênh nhỏ để bật/tắt FLAG_SECURE khi đọc tài liệu số không cho tải: hệ điều hành chặn chụp và quay
 * màn hình, ảnh xem trước trong danh sách ứng dụng gần đây cũng đen. Viết thẳng 20 dòng thay vì
 * thêm một gói phụ thuộc chỉ để gọi một cờ của Window.
 */
class MainActivity : FlutterActivity() {
    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "vn.bluestar.libraryconnect/secure_screen")
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "enable" -> {
                        window.addFlags(WindowManager.LayoutParams.FLAG_SECURE)
                        result.success(true)
                    }
                    "disable" -> {
                        window.clearFlags(WindowManager.LayoutParams.FLAG_SECURE)
                        result.success(true)
                    }
                    else -> result.notImplemented()
                }
            }
    }
}
