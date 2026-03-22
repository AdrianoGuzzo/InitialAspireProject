import 'package:dio/dio.dart';

import '../../../../core/constants/api_constants.dart';
import '../../../../core/error/error_handler.dart';
import '../../../../core/error/result.dart';
import '../../domain/entities/user_profile.dart';
import '../../domain/repositories/profile_repository.dart';
import '../models/change_password_dto.dart';
import '../models/profile_response_dto.dart';
import '../models/update_profile_dto.dart';

class ProfileRepositoryImpl implements ProfileRepository {
  final Dio _dio;

  ProfileRepositoryImpl(this._dio);

  @override
  Future<Result<UserProfile>> getProfile() async {
    try {
      final response = await _dio.get(ApiConstants.profile);
      final dto = ProfileResponseDto.fromJson(
        response.data as Map<String, dynamic>,
      );
      return Result.success(dto.toDomain());
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> updateProfile({required String fullName}) async {
    try {
      await _dio.put(
        ApiConstants.profile,
        data: UpdateProfileDto(fullName: fullName).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }

  @override
  Future<Result<void>> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    try {
      await _dio.post(
        ApiConstants.changePassword,
        data: ChangePasswordDto(
          currentPassword: currentPassword,
          newPassword: newPassword,
        ).toJson(),
      );
      return const Result.success(null);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }
}
