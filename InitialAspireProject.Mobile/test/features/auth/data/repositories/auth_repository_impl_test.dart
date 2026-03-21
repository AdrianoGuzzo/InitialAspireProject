import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/constants/api_constants.dart';
import 'package:initial_aspire_project_mobile/core/error/failures.dart';
import 'package:initial_aspire_project_mobile/core/error/result.dart';
import 'package:initial_aspire_project_mobile/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:initial_aspire_project_mobile/features/auth/domain/entities/auth_tokens.dart';
import 'package:mocktail/mocktail.dart';

class MockDio extends Mock implements Dio {}

void main() {
  late MockDio mockDio;
  late AuthRepositoryImpl repository;

  setUp(() {
    mockDio = MockDio();
    repository = AuthRepositoryImpl(mockDio);
  });

  group('login', () {
    test('returns AuthTokens on success', () async {
      when(() => mockDio.post(ApiConstants.login, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                data: {'token': 'jwt', 'refreshToken': 'rt'},
                statusCode: 200,
                requestOptions: RequestOptions(path: ApiConstants.login),
              ));

      final result = await repository.login(
        email: 'test@test.com',
        password: 'pass',
      );

      expect(result, isA<Success<AuthTokens>>());
      final success = result as Success<AuthTokens>;
      expect(success.data.accessToken, 'jwt');
      expect(success.data.refreshToken, 'rt');
    });

    test('returns EmailNotConfirmedFailure on EmailNotConfirmed code', () async {
      when(() => mockDio.post(ApiConstants.login, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 400,
          data: {'code': 'EmailNotConfirmed', 'message': 'Not confirmed'},
          requestOptions: RequestOptions(path: ApiConstants.login),
        ),
        requestOptions: RequestOptions(path: ApiConstants.login),
      ));

      final result = await repository.login(
        email: 'test@test.com',
        password: 'pass',
      );

      expect(result, isA<ResultFailure<AuthTokens>>());
      final failure = (result as ResultFailure<AuthTokens>).failure;
      expect(failure, isA<EmailNotConfirmedFailure>());
    });

    test('returns NetworkFailure on connection error', () async {
      when(() => mockDio.post(ApiConstants.login, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.connectionError,
        requestOptions: RequestOptions(path: ApiConstants.login),
      ));

      final result = await repository.login(
        email: 'test@test.com',
        password: 'pass',
      );

      expect(result, isA<ResultFailure<AuthTokens>>());
      final failure = (result as ResultFailure<AuthTokens>).failure;
      expect(failure, isA<NetworkFailure>());
    });
  });

  group('register', () {
    test('returns success on 200', () async {
      when(() => mockDio.post(ApiConstants.register, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions: RequestOptions(path: ApiConstants.register),
              ));

      final result = await repository.register(
        email: 'test@test.com',
        password: 'pass',
      );

      expect(result, isA<Success<void>>());
    });
  });

  group('forgotPassword', () {
    test('returns success on 200', () async {
      when(() =>
              mockDio.post(ApiConstants.forgotPassword, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions:
                    RequestOptions(path: ApiConstants.forgotPassword),
              ));

      final result = await repository.forgotPassword(email: 'test@test.com');

      expect(result, isA<Success<void>>());
    });
  });
}
