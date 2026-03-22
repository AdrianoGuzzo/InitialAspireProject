import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/error/error_handler.dart';
import 'package:initial_aspire_project_mobile/core/error/failures.dart';

void main() {
  group('ErrorHandler', () {
    test('maps connection timeout to NetworkFailure', () {
      final error = DioException(
        type: DioExceptionType.connectionTimeout,
        requestOptions: RequestOptions(path: '/test'),
      );

      final result = ErrorHandler.handle(error);
      expect(result, isA<NetworkFailure>());
    });

    test('maps connectionError to NetworkFailure', () {
      final error = DioException(
        type: DioExceptionType.connectionError,
        requestOptions: RequestOptions(path: '/test'),
      );

      final result = ErrorHandler.handle(error);
      expect(result, isA<NetworkFailure>());
    });

    test('maps SocketException to NetworkFailure', () {
      final error = DioException(
        type: DioExceptionType.unknown,
        error: const SocketException('No internet'),
        requestOptions: RequestOptions(path: '/test'),
      );

      final result = ErrorHandler.handle(error);
      expect(result, isA<NetworkFailure>());
    });

    test('maps 401 to UnauthorizedFailure', () {
      final error = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/test'),
        ),
        requestOptions: RequestOptions(path: '/test'),
      );

      final result = ErrorHandler.handle(error);
      expect(result, isA<UnauthorizedFailure>());
    });

    test('maps EmailNotConfirmed response to EmailNotConfirmedFailure', () {
      final error = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 400,
          data: {'code': 'EmailNotConfirmed', 'message': 'Not confirmed'},
          requestOptions: RequestOptions(path: '/test'),
        ),
        requestOptions: RequestOptions(path: '/test'),
      );

      final result = ErrorHandler.handle(error);
      expect(result, isA<EmailNotConfirmedFailure>());
    });

    test('maps validation errors to ValidationFailure', () {
      final error = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 400,
          data: {
            'errors': {
              'Email': ['Invalid email'],
              'Password': ['Too short'],
            },
          },
          requestOptions: RequestOptions(path: '/test'),
        ),
        requestOptions: RequestOptions(path: '/test'),
      );

      final result = ErrorHandler.handle(error);
      expect(result, isA<ValidationFailure>());
      final validation = result as ValidationFailure;
      expect(validation.errors['Email'], ['Invalid email']);
      expect(validation.errors['Password'], ['Too short']);
    });

    test('maps server error with message to ServerFailure', () {
      final error = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 500,
          data: {'message': 'Internal error'},
          requestOptions: RequestOptions(path: '/test'),
        ),
        requestOptions: RequestOptions(path: '/test'),
      );

      final result = ErrorHandler.handle(error);
      expect(result, isA<ServerFailure>());
      expect((result as ServerFailure).message, 'Internal error');
    });

    test('maps non-DioException to UnknownFailure', () {
      final result = ErrorHandler.handle(Exception('something'));
      expect(result, isA<UnknownFailure>());
    });
  });
}
