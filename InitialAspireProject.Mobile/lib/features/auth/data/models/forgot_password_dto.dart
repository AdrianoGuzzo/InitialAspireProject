import 'package:json_annotation/json_annotation.dart';

part 'forgot_password_dto.g.dart';

@JsonSerializable(createFactory: false)
class ForgotPasswordDto {
  final String email;

  const ForgotPasswordDto({required this.email});

  Map<String, dynamic> toJson() => _$ForgotPasswordDtoToJson(this);
}
