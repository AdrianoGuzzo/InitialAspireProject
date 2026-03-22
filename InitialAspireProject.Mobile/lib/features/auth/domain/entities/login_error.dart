import 'package:freezed_annotation/freezed_annotation.dart';

part 'login_error.freezed.dart';

@freezed
class LoginError with _$LoginError {
  const factory LoginError({
    required String code,
    required String message,
  }) = _LoginError;
}
