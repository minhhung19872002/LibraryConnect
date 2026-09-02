import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  // Be Vietnam Pro và Lora đóng gói trong assets/google_fonts: không tải từ mạng, mất mạng vẫn
  // đúng phông (trước đây google_fonts gọi fonts.gstatic.com mỗi lần mở và rơi về phông hệ thống).
  GoogleFonts.config.allowRuntimeFetching = false;
  runApp(const ProviderScope(child: LibraryConnectApp()));
}
