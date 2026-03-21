import 'package:json_annotation/json_annotation.dart';

part 'update_profile_dto.g.dart';

@JsonSerializable()
class UpdateProfileDto {
  final String fullName;

  const UpdateProfileDto({required this.fullName});

  Map<String, dynamic> toJson() => _$UpdateProfileDtoToJson(this);
}
