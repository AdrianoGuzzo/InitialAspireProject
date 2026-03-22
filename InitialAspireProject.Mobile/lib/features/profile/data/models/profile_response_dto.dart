import 'package:json_annotation/json_annotation.dart';

import '../../domain/entities/user_profile.dart';

part 'profile_response_dto.g.dart';

@JsonSerializable(createToJson: false)
class ProfileResponseDto {
  final String email;
  final String? fullName;
  final List<String> roles;

  const ProfileResponseDto({
    required this.email,
    this.fullName,
    this.roles = const [],
  });

  factory ProfileResponseDto.fromJson(Map<String, dynamic> json) =>
      _$ProfileResponseDtoFromJson(json);

  UserProfile toDomain() => UserProfile(
        email: email,
        fullName: fullName ?? '',
        roles: roles,
      );
}
