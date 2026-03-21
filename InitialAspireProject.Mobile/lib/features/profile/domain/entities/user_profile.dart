import 'package:freezed_annotation/freezed_annotation.dart';

part 'user_profile.freezed.dart';

@freezed
class UserProfile with _$UserProfile {
  const factory UserProfile({
    required String email,
    required String fullName,
    required List<String> roles,
  }) = _UserProfile;
}
