import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';

/// Ô báo lỗi của khung quét, dùng chung cho mọi màn hình có camera.
///
/// Chỉ **một** loại lỗi được nói là "chưa được phép dùng camera": lỗi quyền. Camera đang bận (màn
/// hình trước chưa nhả), thiết bị không có camera, hay bộ quét không khởi động được là chuyện khác
/// hẳn — bảo bạn đọc đi cấp một quyền họ đã cấp rồi thì họ vào Cài đặt, thấy quyền đang bật, và
/// không còn đường nào đi tiếp. Màn Mượn tự phục vụ từng nói câu ấy cho mọi lỗi (K16, 05/09/2026).
///
/// Gói vào một widget để chỗ thứ ba có camera không phải nhớ lại luật này.
class CameraErrorView extends StatelessWidget {
  const CameraErrorView({super.key, required this.error, this.onEnterCode});

  final MobileScannerException error;

  /// Lối đi tiếp khi không quét được: nhập mã bằng tay. Không có thì chỉ hiện lời báo.
  final VoidCallback? onEnterCode;

  /// Lời báo đúng nguyên nhân: quyền thì nói về quyền, còn lại thì nói lỗi thật của bộ quét.
  static String messageFor(MobileScannerException error, L10n l10n) =>
      error.errorCode == MobileScannerErrorCode.permissionDenied
      ? l10n.scanCameraDenied
      : error.errorDetails?.message ?? l10n.scanCameraUnavailable;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);

    return ColoredBox(
      color: LcColors.greenDark,
      child: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.no_photography_outlined,
                size: 48,
                color: LcColors.cream,
              ),
              const SizedBox(height: 12),
              Text(
                messageFor(error, l10n),
                textAlign: TextAlign.center,
                style: const TextStyle(color: LcColors.cream),
              ),
              if (onEnterCode != null) ...[
                const SizedBox(height: 12),
                FilledButton.tonal(
                  onPressed: onEnterCode,
                  child: Text(l10n.scanEnterCode),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
