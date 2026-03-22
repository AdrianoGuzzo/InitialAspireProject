import 'package:json_annotation/json_annotation.dart';

part 'confirm_email_dto.g.dart';

@JsonSerializable(createFactory: false)
class ConfirmEmailDto {
  final String email;
  final String token;

  const ConfirmEmailDto({required this.email, required this.token});

  Map<String, dynamic> toJson() => _$ConfirmEmailDtoToJson(this);
}
