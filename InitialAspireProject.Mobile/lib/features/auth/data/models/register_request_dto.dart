import 'package:json_annotation/json_annotation.dart';

part 'register_request_dto.g.dart';

@JsonSerializable(createFactory: false)
class RegisterRequestDto {
  final String email;
  final String password;
  final String? fullName;

  const RegisterRequestDto({
    required this.email,
    required this.password,
    this.fullName,
  });

  Map<String, dynamic> toJson() => _$RegisterRequestDtoToJson(this);
}
