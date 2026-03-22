import 'package:json_annotation/json_annotation.dart';

part 'change_password_dto.g.dart';

@JsonSerializable(createFactory: false)
class ChangePasswordDto {
  final String currentPassword;
  final String newPassword;

  const ChangePasswordDto({
    required this.currentPassword,
    required this.newPassword,
  });

  Map<String, dynamic> toJson() => _$ChangePasswordDtoToJson(this);
}
