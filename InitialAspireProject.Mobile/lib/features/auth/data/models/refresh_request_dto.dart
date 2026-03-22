import 'package:json_annotation/json_annotation.dart';

part 'refresh_request_dto.g.dart';

@JsonSerializable(createFactory: false)
class RefreshRequestDto {
  final String refreshToken;

  const RefreshRequestDto({required this.refreshToken});

  Map<String, dynamic> toJson() => _$RefreshRequestDtoToJson(this);
}
