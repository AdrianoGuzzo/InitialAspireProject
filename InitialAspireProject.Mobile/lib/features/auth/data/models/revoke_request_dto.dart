import 'package:json_annotation/json_annotation.dart';

part 'revoke_request_dto.g.dart';

@JsonSerializable(createFactory: false)
class RevokeRequestDto {
  final String refreshToken;

  const RevokeRequestDto({required this.refreshToken});

  Map<String, dynamic> toJson() => _$RevokeRequestDtoToJson(this);
}
