import '../../../../core/error/result.dart';
import '../entities/auth_tokens.dart';

abstract class AuthRepository {
  Future<Result<AuthTokens>> login({
    required String email,
    required String password,
  });

  Future<Result<void>> register({
    required String email,
    required String password,
    String? fullName,
  });

  Future<Result<AuthTokens>> refresh({required String refreshToken});

  Future<Result<void>> revoke({required String refreshToken});

  Future<Result<void>> forgotPassword({required String email});

  Future<Result<void>> resetPassword({
    required String email,
    required String token,
    required String newPassword,
    required String confirmPassword,
  });

  Future<Result<void>> confirmEmail({
    required String email,
    required String token,
  });

  Future<Result<void>> resendActivation({required String email});
}
