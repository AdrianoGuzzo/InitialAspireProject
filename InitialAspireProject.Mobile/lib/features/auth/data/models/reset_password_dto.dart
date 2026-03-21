import 'package:json_annotation/json_annotation.dart';

part 'reset_password_dto.g.dart';

@JsonSerializable()
class ResetPasswordDto {
  final String email;
  final String token;
  final String newPassword;
  final String confirmPassword;

  const ResetPasswordDto({
    required this.email,
    required this.token,
    required this.newPassword,
    required this.confirmPassword,
  });

  Map<String, dynamic> toJson() => _$ResetPasswordDtoToJson(this);
}
