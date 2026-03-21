import '../../../../core/error/result.dart';
import '../entities/user_profile.dart';

abstract class ProfileRepository {
  Future<Result<UserProfile>> getProfile();
  Future<Result<void>> updateProfile({required String fullName});
  Future<Result<void>> changePassword({
    required String currentPassword,
    required String newPassword,
  });
}
