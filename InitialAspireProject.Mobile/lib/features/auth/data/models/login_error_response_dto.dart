import 'package:json_annotation/json_annotation.dart';

import '../../domain/entities/login_error.dart';

part 'login_error_response_dto.g.dart';

@JsonSerializable(createToJson: false)
class LoginErrorResponseDto {
  final String code;
  final String message;

  const LoginErrorResponseDto({required this.code, required this.message});

  factory LoginErrorResponseDto.fromJson(Map<String, dynamic> json) =>
      _$LoginErrorResponseDtoFromJson(json);

  LoginError toDomain() => LoginError(code: code, message: message);
}
