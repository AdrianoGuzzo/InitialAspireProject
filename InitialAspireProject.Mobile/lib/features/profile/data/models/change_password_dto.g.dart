// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'change_password_dto.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ChangePasswordDto _$ChangePasswordDtoFromJson(Map<String, dynamic> json) =>
    ChangePasswordDto(
      currentPassword: json['currentPassword'] as String,
      newPassword: json['newPassword'] as String,
    );

Map<String, dynamic> _$ChangePasswordDtoToJson(ChangePasswordDto instance) =>
    <String, dynamic>{
      'currentPassword': instance.currentPassword,
      'newPassword': instance.newPassword,
    };
