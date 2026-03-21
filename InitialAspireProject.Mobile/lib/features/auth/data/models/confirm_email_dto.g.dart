// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'confirm_email_dto.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ConfirmEmailDto _$ConfirmEmailDtoFromJson(Map<String, dynamic> json) =>
    ConfirmEmailDto(
      email: json['email'] as String,
      token: json['token'] as String,
    );

Map<String, dynamic> _$ConfirmEmailDtoToJson(ConfirmEmailDto instance) =>
    <String, dynamic>{
      'email': instance.email,
      'token': instance.token,
    };
