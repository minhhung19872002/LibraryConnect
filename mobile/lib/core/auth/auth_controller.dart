import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../shared/models/auth_models.dart';
import '../api/api_client.dart';
import '../api/api_exception.dart';

/// Trạng thái phiên bạn đọc: chưa biết (đang đọc kho token), đã đăng nhập, hoặc khách.
sealed class AuthState {
  const AuthState();
}

class AuthLoading extends AuthState {
  const AuthLoading();
}

class AuthGuest extends AuthState {
  const AuthGuest();
}

/// Có phiên trên máy nhưng bạn đọc bật khoá sinh trắc học và chưa xác thực lần mở này.
class AuthLocked extends AuthState {
  const AuthLocked();
}

class AuthSignedIn extends AuthState {
  const AuthSignedIn(this.user, {this.mustChangePassword = false});

  final AuthUser user;
  final bool mustChangePassword;
}

/// Đăng nhập, đăng xuất và khôi phục phiên khi mở ứng dụng.
///
/// Ứng dụng không tự quyết gì về thẻ hay quyền: máy chủ trả token thì là đăng nhập được, máy chủ
/// từ chối thì hiện đúng câu máy chủ nói. Khi làm mới token thất bại ở bất kỳ lượt gọi nào,
/// [ApiClient] tăng [sessionExpiredProvider] và bộ điều khiển này đưa về trạng thái khách.
class AuthController extends Notifier<AuthState> {
  @override
  AuthState build() {
    ref.listen(sessionExpiredProvider, (previous, next) {
      if (next != previous && state is AuthSignedIn) {
        state = const AuthGuest();
      }
    });

    _restore();
    return const AuthLoading();
  }

  Future<void> _restore() async {
    final tokens = ref.read(tokenStoreProvider);

    if (!await tokens.hasSession) {
      state = const AuthGuest();
      return;
    }

    // Khoá sinh trắc học: có phiên nhưng phải xác thực vân tay / khuôn mặt mới vào tài khoản.
    if (await tokens.biometricEnabled) {
      state = const AuthLocked();
      return;
    }

    await _restoreSession();
  }

  /// Xác thực sinh trắc học rồi mở phiên đã lưu. Trả về false khi người dùng huỷ hoặc máy từ chối.
  Future<bool> unlock(Future<bool> Function() authenticate) async {
    if (state is! AuthLocked) return state is AuthSignedIn;
    bool ok;
    try {
      ok = await authenticate();
    } catch (_) {
      ok = false;
    }
    if (!ok) return false;
    await _restoreSession();
    return state is AuthSignedIn;
  }

  Future<void> _restoreSession() async {
    final tokens = ref.read(tokenStoreProvider);

    try {
      final api = ref.read(apiClientProvider);
      final profile = await api.get<Map<String, dynamic>>(
        '/reader/profile',
        decode: (json) => json as Map<String, dynamic>,
      );

      state = AuthSignedIn(
        AuthUser(
          id: profile['id'] as String,
          username: profile['cardNumber'] as String,
          fullName: profile['fullName'] as String,
          email: profile['email'] as String?,
          avatarUrl: profile['photoUrl'] as String?,
          isReader: true,
        ),
      );
    } on ApiException catch (error) {
      // Mất mạng lúc mở ứng dụng thì vẫn coi là đã đăng nhập: thẻ điện tử và danh sách đang mượn
      // đọc từ bộ nhớ đệm; lượt gọi tiếp theo sẽ làm mới token khi có mạng.
      if (error.isNetwork || error.kind == ApiErrorKind.timeout) {
        final card = await tokens.card;
        state = AuthSignedIn(
          AuthUser(
            id: card?['readerId'] as String? ?? '',
            username: card?['cardNumber'] as String? ?? '',
            fullName: card?['fullName'] as String? ?? '',
            isReader: true,
          ),
        );
        return;
      }

      await tokens.clear();
      state = const AuthGuest();
    }
  }

  /// Đăng nhập bằng số thẻ và mật khẩu. Ném [ApiException] với câu của máy chủ khi sai.
  Future<AuthResult> signIn({
    required String cardNumber,
    required String password,
    bool remember = true,
  }) async {
    final api = ref.read(apiClientProvider);
    final tokens = ref.read(tokenStoreProvider);

    final result = await api.post<AuthResult>(
      '/reader/auth/login',
      body: {'cardNumber': cardNumber.trim(), 'password': password},
      anonymous: true,
      decode: (json) => AuthResult.fromJson(json as Map<String, dynamic>),
    );

    await tokens.save(
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
    );
    await tokens.rememberCardNumber(remember ? cardNumber.trim() : null);

    state = AuthSignedIn(
      result.user,
      mustChangePassword: result.mustChangePassword,
    );
    return result;
  }

  Future<void> signOut({String? deviceToken}) async {
    final api = ref.read(apiClientProvider);
    final tokens = ref.read(tokenStoreProvider);

    if (deviceToken != null && deviceToken.isNotEmpty) {
      try {
        await api.delete<dynamic>(
          '/reader/devices',
          query: {'token': deviceToken},
        );
      } on ApiException {
        // Không gỡ được thiết bị (mất mạng) thì máy chủ sẽ tự gỡ khi Firebase báo mã chết.
      }
    }

    await tokens.clear();
    state = const AuthGuest();
  }

  void passwordChanged() {
    if (state case AuthSignedIn(:final user)) {
      state = AuthSignedIn(user);
    }
  }
}

final authControllerProvider = NotifierProvider<AuthController, AuthState>(
  AuthController.new,
);

/// Tiện: bạn đọc hiện tại hoặc null.
final currentReaderProvider = Provider<AuthUser?>((ref) {
  final state = ref.watch(authControllerProvider);
  return state is AuthSignedIn ? state.user : null;
});
