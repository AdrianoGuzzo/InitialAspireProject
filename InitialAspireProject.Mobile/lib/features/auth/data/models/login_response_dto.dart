import 'package:json_annotation/json_annotation.dart';

import '../../domain/entities/auth_tokens.dart';

part 'login_response_dto.g.dart';

@JsonSerializable(createToJson: false)
class LoginResponseDto {
  final String token;
  final String refreshToken;

  const LoginResponseDto({required this.token, required this.refreshToken});

  factory LoginResponseDto.fromJson(Map<String, dynamic> json) =>
      _$LoginResponseDtoFromJson(json);

  AuthTokens toDomain() => AuthTokens(
        accessToken: token,
        refreshToken: refreshToken,
      );
}
