import 'dart:io';

import 'package:dio/dio.dart';

import 'failures.dart';

class ErrorHandler {
  ErrorHandler._();

  static Failure handle(Object error) {
    if (error is DioException) {
      return _handleDioException(error);
    }
    return Failure.unknown(message: error.toString());
  }

  static Failure _handleDioException(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.connectionError:
        return const Failure.network();
      case DioExceptionType.badResponse:
        return _handleBadResponse(e.response);
      case DioExceptionType.cancel:
        return const Failure.unknown(message: 'Request cancelled');
      default:
        if (e.error is SocketException) {
          return const Failure.network();
        }
        return Failure.unknown(message: e.message);
    }
  }

  static Failure _handleBadResponse(Response? response) {
    if (response == null) {
      return const Failure.unknown();
    }

    final statusCode = response.statusCode ?? 0;
    final data = response.data;

    if (statusCode == 401) {
      return const Failure.unauthorized();
    }

    if (data is Map<String, dynamic>) {
      // Login error with code
      final code = data['code'] as String?;
      if (code == 'EmailNotConfirmed') {
        return Failure.emailNotConfirmed(message: data['message'] as String?);
      }

      // Validation errors from ASP.NET
      final errors = data['errors'];
      if (errors is Map<String, dynamic>) {
        final parsed = errors.map(
          (key, value) => MapEntry(
            key,
            (value is List) ? value.cast<String>() : [value.toString()],
          ),
        );
        return Failure.validation(errors: parsed);
      }

      final message = data['message'] as String? ?? data['title'] as String?;
      if (message != null) {
        return Failure.server(message: message);
      }
    }

    return Failure.server(
      message: 'HTTP $statusCode',
    );
  }
}
