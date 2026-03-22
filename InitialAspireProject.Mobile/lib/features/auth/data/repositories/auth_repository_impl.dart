import 'package:dio/dio.dart';

import '../../../../core/constants/api_constants.dart';
import '../../../../core/error/error_handler.dart';
import '../../../../core/error/failures.dart';
import '../../../../core/error/result.dart';
import '../../domain/entities/auth_tokens.dart';
import '../../domain/repositories/auth_repository.dart';
import '../models/confirm_email_dto.dart';
import '../models/forgot_password_dto.dart';
import '../models/login_request_dto.dart';
import '../models/login_response_dto.dart';
import '../models/refresh_request_dto.dart';
import '../models/register_request_dto.dart';
import '../models/reset_password_dto.dart';
import '../models/revoke_request_dto.dart';

class AuthRepositoryImpl implements AuthRepository {
  final Dio _dio;

  AuthRepositoryImpl(this._dio);

  @override
  Future<Result<AuthTokens>> login({
    required String email,
    required String password,
  }) async {
    try {
      final response = await _dio.post(
        ApiConstants.login,
        data: LoginRequestDto(email: email, password: password).toJson(),
      );

      final dto = LoginResponseDto.fromJson(response.data as Map<String, dynamic>);
      return Result.success(dto.toDomain());
    } on DioException catch (e) {
      // Check for EmailNotConfirmed before generic handling
      final data = e.response?.data;
      if (data is Map<String, dynamic> && data['code'] == 'EmailNotConfirmed') {
        return Result.failure(
          Failure.emailNotConfirmed(message: data['message'] as String?),
        );
      }
      return Result.failure(ErrorHandler.handle(e));
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> register({
    required String email,
    required String password,
    String? fullName,
  }) async {
    try {
      await _dio.post(
        ApiConstants.register,
        data: RegisterRequestDto(
          email: email,
          password: password,
          fullName: fullName,
        ).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<AuthTokens>> refresh({required String refreshToken}) async {
    try {
      final response = await _dio.post(
        ApiConstants.refresh,
        data: RefreshRequestDto(refreshToken: refreshToken).toJson(),
      );
      final dto = LoginResponseDto.fromJson(response.data as Map<String, dynamic>);
      return Result.success(dto.toDomain());
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> revoke({required String refreshToken}) async {
    try {
      await _dio.post(
        ApiConstants.revoke,
        data: RevokeRequestDto(refreshToken: refreshToken).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> forgotPassword({required String email}) async {
    try {
      await _dio.post(
        ApiConstants.forgotPassword,
        data: ForgotPasswordDto(email: email).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> resetPassword({
    required String email,
    required String token,
    required String newPassword,
    required String confirmPassword,
  }) async {
    try {
      await _dio.post(
        ApiConstants.resetPassword,
        data: ResetPasswordDto(
          email: email,
          token: token,
          newPassword: newPassword,
          confirmPassword: confirmPassword,
        ).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> confirmEmail({
    required String email,
    required String token,
  }) async {
    try {
      await _dio.post(
        ApiConstants.confirmEmail,
        data: ConfirmEmailDto(email: email, token: token).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> resendActivation({required String email}) async {
    try {
      await _dio.post(
        ApiConstants.resendActivation,
        data: ForgotPasswordDto(email: email).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }
}
