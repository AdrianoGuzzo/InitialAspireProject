import 'package:freezed_annotation/freezed_annotation.dart';

part 'failures.freezed.dart';

@freezed
sealed class Failure with _$Failure {
  const factory Failure.server({String? message}) = ServerFailure;
  const factory Failure.network() = NetworkFailure;
  const factory Failure.unauthorized() = UnauthorizedFailure;
  const factory Failure.validation({required Map<String, List<String>> errors}) =
      ValidationFailure;
  const factory Failure.emailNotConfirmed({String? message}) =
      EmailNotConfirmedFailure;
  const factory Failure.unknown({String? message}) = UnknownFailure;
}
