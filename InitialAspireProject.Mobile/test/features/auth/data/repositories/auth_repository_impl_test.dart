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

    test('returns ServerFailure with message on InvalidCredentials code',
        () async {
      when(() => mockDio.post(ApiConstants.login, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          data: {
            'code': 'InvalidCredentials',
            'message': 'Invalid credentials'
          },
          requestOptions: RequestOptions(path: ApiConstants.login),
        ),
        requestOptions: RequestOptions(path: ApiConstants.login),
      ));

      final result = await repository.login(
        email: 'test@test.com',
        password: 'wrong',
      );

      expect(result, isA<ResultFailure<AuthTokens>>());
      final failure = (result as ResultFailure<AuthTokens>).failure;
      expect(failure, isA<ServerFailure>());
      expect((failure as ServerFailure).message, 'Invalid credentials');
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

    test('returns ValidationFailure on 400 with errors', () async {
      when(() => mockDio.post(ApiConstants.register, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 400,
          data: {
            'errors': {
              'Email': ['Email already in use'],
            },
          },
          requestOptions: RequestOptions(path: ApiConstants.register),
        ),
        requestOptions: RequestOptions(path: ApiConstants.register),
      ));

      final result = await repository.register(
        email: 'test@test.com',
        password: 'pass',
      );

      expect(result, isA<ResultFailure<void>>());
      final failure = (result as ResultFailure<void>).failure;
      expect(failure, isA<ValidationFailure>());
    });

    test('returns NetworkFailure on connection error', () async {
      when(() => mockDio.post(ApiConstants.register, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.connectionError,
        requestOptions: RequestOptions(path: ApiConstants.register),
      ));

      final result = await repository.register(
        email: 'test@test.com',
        password: 'pass',
      );

      expect(result, isA<ResultFailure<void>>());
      final failure = (result as ResultFailure<void>).failure;
      expect(failure, isA<NetworkFailure>());
    });
  });

  group('refresh', () {
    test('returns AuthTokens on success', () async {
      when(() => mockDio.post(ApiConstants.refresh, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                data: {'token': 'new-jwt', 'refreshToken': 'new-rt'},
                statusCode: 200,
                requestOptions: RequestOptions(path: ApiConstants.refresh),
              ));

      final result = await repository.refresh(refreshToken: 'old-rt');

      expect(result, isA<Success<AuthTokens>>());
      final tokens = (result as Success<AuthTokens>).data;
      expect(tokens.accessToken, 'new-jwt');
      expect(tokens.refreshToken, 'new-rt');
    });

    test('returns NetworkFailure on connection error', () async {
      when(() => mockDio.post(ApiConstants.refresh, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.connectionError,
        requestOptions: RequestOptions(path: ApiConstants.refresh),
      ));

      final result = await repository.refresh(refreshToken: 'rt');

      expect(result, isA<ResultFailure<AuthTokens>>());
      final failure = (result as ResultFailure<AuthTokens>).failure;
      expect(failure, isA<NetworkFailure>());
    });
  });

  group('revoke', () {
    test('returns success on 200', () async {
      when(() => mockDio.post(ApiConstants.revoke, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions: RequestOptions(path: ApiConstants.revoke),
              ));

      final result = await repository.revoke(refreshToken: 'rt');

      expect(result, isA<Success<void>>());
    });

    test('returns failure on error', () async {
      when(() => mockDio.post(ApiConstants.revoke, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.connectionError,
        requestOptions: RequestOptions(path: ApiConstants.revoke),
      ));

      final result = await repository.revoke(refreshToken: 'rt');

      expect(result, isA<ResultFailure<void>>());
    });
  });

  group('resetPassword', () {
    test('returns success on 200', () async {
      when(() =>
              mockDio.post(ApiConstants.resetPassword, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions:
                    RequestOptions(path: ApiConstants.resetPassword),
              ));

      final result = await repository.resetPassword(
        email: 'test@test.com',
        token: 'token',
        newPassword: 'new-pass',
        confirmPassword: 'new-pass',
      );

      expect(result, isA<Success<void>>());
    });

    test('returns ValidationFailure on 400', () async {
      when(() =>
              mockDio.post(ApiConstants.resetPassword, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 400,
          data: {
            'errors': {
              'NewPassword': ['Password too weak'],
            },
          },
          requestOptions:
              RequestOptions(path: ApiConstants.resetPassword),
        ),
        requestOptions: RequestOptions(path: ApiConstants.resetPassword),
      ));

      final result = await repository.resetPassword(
        email: 'test@test.com',
        token: 'token',
        newPassword: 'weak',
        confirmPassword: 'weak',
      );

      expect(result, isA<ResultFailure<void>>());
      final failure = (result as ResultFailure<void>).failure;
      expect(failure, isA<ValidationFailure>());
    });
  });

  group('confirmEmail', () {
    test('returns success on 200', () async {
      when(() =>
              mockDio.post(ApiConstants.confirmEmail, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions:
                    RequestOptions(path: ApiConstants.confirmEmail),
              ));

      final result = await repository.confirmEmail(
        email: 'test@test.com',
        token: 'token',
      );

      expect(result, isA<Success<void>>());
    });

    test('returns ServerFailure on bad token', () async {
      when(() =>
              mockDio.post(ApiConstants.confirmEmail, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 400,
          data: {'message': 'Invalid token'},
          requestOptions:
              RequestOptions(path: ApiConstants.confirmEmail),
        ),
        requestOptions: RequestOptions(path: ApiConstants.confirmEmail),
      ));

      final result = await repository.confirmEmail(
        email: 'test@test.com',
        token: 'bad-token',
      );

      expect(result, isA<ResultFailure<void>>());
      final failure = (result as ResultFailure<void>).failure;
      expect(failure, isA<ServerFailure>());
    });
  });

  group('resendActivation', () {
    test('returns success on 200', () async {
      when(() => mockDio.post(ApiConstants.resendActivation,
              data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions:
                    RequestOptions(path: ApiConstants.resendActivation),
              ));

      final result = await repository.resendActivation(email: 'test@test.com');

      expect(result, isA<Success<void>>());
    });

    test('returns NetworkFailure on connection error', () async {
      when(() => mockDio.post(ApiConstants.resendActivation,
              data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.connectionError,
        requestOptions:
            RequestOptions(path: ApiConstants.resendActivation),
      ));

      final result = await repository.resendActivation(email: 'test@test.com');

      expect(result, isA<ResultFailure<void>>());
      final failure = (result as ResultFailure<void>).failure;
      expect(failure, isA<NetworkFailure>());
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

    test('returns NetworkFailure on connection error', () async {
      when(() =>
              mockDio.post(ApiConstants.forgotPassword, data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.connectionError,
        requestOptions:
            RequestOptions(path: ApiConstants.forgotPassword),
      ));

      final result = await repository.forgotPassword(email: 'test@test.com');

      expect(result, isA<ResultFailure<void>>());
      final failure = (result as ResultFailure<void>).failure;
      expect(failure, isA<NetworkFailure>());
    });
  });
}
