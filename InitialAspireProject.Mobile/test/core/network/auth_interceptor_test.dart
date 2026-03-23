import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/constants/api_constants.dart';
import 'package:initial_aspire_project_mobile/core/network/auth_interceptor.dart';
import 'package:initial_aspire_project_mobile/core/storage/token_storage.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}

class MockDio extends Mock implements Dio {}

class MockRequestInterceptorHandler extends Mock
    implements RequestInterceptorHandler {}

class MockErrorInterceptorHandler extends Mock
    implements ErrorInterceptorHandler {}

void main() {
  late MockFlutterSecureStorage mockStorage;
  late TokenStorage tokenStorage;
  late MockDio mockRefreshDio;
  late AuthInterceptor interceptor;
  late MockRequestInterceptorHandler handler;
  late bool forceLogoutCalled;

  setUpAll(() {
    registerFallbackValue(RequestOptions(path: ''));
    registerFallbackValue(DioException(requestOptions: RequestOptions(path: '')));
    registerFallbackValue(Response(requestOptions: RequestOptions(path: '')));
  });

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    tokenStorage = TokenStorage(mockStorage);
    mockRefreshDio = MockDio();
    handler = MockRequestInterceptorHandler();
    forceLogoutCalled = false;

    interceptor = AuthInterceptor(
      tokenStorage: tokenStorage,
      refreshDio: mockRefreshDio,
      onForceLogout: () {
        forceLogoutCalled = true;
      },
    );
  });

  group('onRequest', () {
    test('adds Bearer token for non-public paths', () async {
      when(() => mockStorage.read(key: any(named: 'key')))
          .thenAnswer((_) async => 'test-token');

      RequestOptions? capturedOptions;

      when(() => handler.next(any())).thenAnswer((invocation) {
        capturedOptions =
            invocation.positionalArguments.first as RequestOptions;
      });

      final options = RequestOptions(path: ApiConstants.weather);

      interceptor.onRequest(options, handler);

      await Future.delayed(Duration.zero);

      verify(() => handler.next(any())).called(1);
      expect(capturedOptions, isNotNull);
      expect(
        capturedOptions!.headers['Authorization'],
        'Bearer test-token',
      );
    });

    test('does not add Bearer token for public paths', () async {
      RequestOptions? capturedOptions;

      when(() => handler.next(any())).thenAnswer((invocation) {
        capturedOptions =
            invocation.positionalArguments.first as RequestOptions;
      });

      final options = RequestOptions(path: ApiConstants.login);

      interceptor.onRequest(options, handler);

      await Future.delayed(Duration.zero);

      verify(() => handler.next(any())).called(1);
      expect(capturedOptions, isNotNull);
      expect(capturedOptions!.headers['Authorization'], isNull);
    });
  });

  group('onError', () {
    late MockErrorInterceptorHandler errorHandler;

    setUp(() {
      errorHandler = MockErrorInterceptorHandler();
    });

    test('non-401 errors pass through', () async {
      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 500,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(Duration.zero);

      verify(() => errorHandler.next(err)).called(1);
    });

    test('already-retried requests pass through', () async {
      final opts = RequestOptions(path: '/api/weather');
      opts.extra['retried'] = true;
      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: opts,
        ),
        requestOptions: opts,
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(Duration.zero);

      verify(() => errorHandler.next(err)).called(1);
    });

    test('auth endpoint (login) is not retried', () async {
      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: ApiConstants.login),
        ),
        requestOptions: RequestOptions(path: ApiConstants.login),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(Duration.zero);

      verify(() => errorHandler.next(err)).called(1);
    });

    test('auth endpoint (refresh) is not retried', () async {
      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: ApiConstants.refresh),
        ),
        requestOptions: RequestOptions(path: ApiConstants.refresh),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(Duration.zero);

      verify(() => errorHandler.next(err)).called(1);
    });

    test('401 with no refresh token triggers force logout', () async {
      // readRefreshToken returns null
      when(() => mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => null);
      // clear() writes
      when(() => mockStorage.delete(key: any(named: 'key')))
          .thenAnswer((_) async {});

      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(const Duration(milliseconds: 50));

      expect(forceLogoutCalled, isTrue);
      verify(() => errorHandler.next(err)).called(1);
    });

    test('401 with successful refresh retries original request', () async {
      // readRefreshToken
      when(() => mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => 'old-rt');
      // refresh call succeeds
      when(() => mockRefreshDio.post(ApiConstants.refresh,
              data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                data: {'token': 'new-jwt', 'refreshToken': 'new-rt'},
                requestOptions: RequestOptions(path: ApiConstants.refresh),
              ));
      // writeTokens
      when(() => mockStorage.write(
            key: any(named: 'key'),
            value: any(named: 'value'),
          )).thenAnswer((_) async {});
      // readAccessToken for retry
      when(() => mockStorage.read(key: 'access_token'))
          .thenAnswer((_) async => 'new-jwt');
      // retry fetch succeeds
      final retryResponse = Response(
        statusCode: 200,
        data: {'forecasts': []},
        requestOptions: RequestOptions(path: '/api/weather'),
      );
      when(() => mockRefreshDio.fetch<dynamic>(any()))
          .thenAnswer((_) async => retryResponse);

      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(const Duration(milliseconds: 50));

      verify(() => errorHandler.resolve(retryResponse)).called(1);
      expect(forceLogoutCalled, isFalse);
    });

    test('401 with refresh API failure triggers force logout', () async {
      when(() => mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => 'old-rt');
      when(() => mockRefreshDio.post(ApiConstants.refresh,
              data: any(named: 'data')))
          .thenThrow(DioException(
        type: DioExceptionType.connectionError,
        requestOptions: RequestOptions(path: ApiConstants.refresh),
      ));
      when(() => mockStorage.delete(key: any(named: 'key')))
          .thenAnswer((_) async {});

      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(const Duration(milliseconds: 50));

      expect(forceLogoutCalled, isTrue);
      verify(() => errorHandler.next(err)).called(1);
    });

    test('401 with refresh returning non-200 triggers force logout', () async {
      when(() => mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => 'old-rt');
      when(() => mockRefreshDio.post(ApiConstants.refresh,
              data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 401,
                data: {'code': 'Expired'},
                requestOptions: RequestOptions(path: ApiConstants.refresh),
              ));
      when(() => mockStorage.delete(key: any(named: 'key')))
          .thenAnswer((_) async {});

      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(const Duration(milliseconds: 50));

      expect(forceLogoutCalled, isTrue);
      verify(() => errorHandler.next(err)).called(1);
    });

    test('401 refresh succeeds but retry fails passes retry error', () async {
      when(() => mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => 'old-rt');
      when(() => mockRefreshDio.post(ApiConstants.refresh,
              data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                data: {'token': 'new-jwt', 'refreshToken': 'new-rt'},
                requestOptions: RequestOptions(path: ApiConstants.refresh),
              ));
      when(() => mockStorage.write(
            key: any(named: 'key'),
            value: any(named: 'value'),
          )).thenAnswer((_) async {});
      when(() => mockStorage.read(key: 'access_token'))
          .thenAnswer((_) async => 'new-jwt');

      final retryError = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 403,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );
      when(() => mockRefreshDio.fetch<dynamic>(any())).thenThrow(retryError);

      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );

      interceptor.onError(err, errorHandler);

      await Future.delayed(const Duration(milliseconds: 50));

      verify(() => errorHandler.next(retryError)).called(1);
    });

    test('concurrent 401s trigger only one refresh call', () async {
      final refreshCompleter = Completer<Response>();

      when(() => mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => 'old-rt');
      when(() => mockRefreshDio.post(ApiConstants.refresh,
              data: any(named: 'data')))
          .thenAnswer((_) => refreshCompleter.future);
      when(() => mockStorage.write(
            key: any(named: 'key'),
            value: any(named: 'value'),
          )).thenAnswer((_) async {});
      when(() => mockStorage.read(key: 'access_token'))
          .thenAnswer((_) async => 'new-jwt');
      when(() => mockRefreshDio.fetch<dynamic>(any()))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions: RequestOptions(path: '/api/weather'),
              ));

      final errorHandler1 = MockErrorInterceptorHandler();
      final errorHandler2 = MockErrorInterceptorHandler();

      final err1 = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );
      final err2 = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/profile'),
        ),
        requestOptions: RequestOptions(path: '/api/profile'),
      );

      // Fire both 401s
      interceptor.onError(err1, errorHandler1);
      interceptor.onError(err2, errorHandler2);

      // Let microtasks run so both enter _tryRefresh
      await Future.delayed(Duration.zero);

      // Complete the single refresh
      refreshCompleter.complete(Response(
        statusCode: 200,
        data: {'token': 'new-jwt', 'refreshToken': 'new-rt'},
        requestOptions: RequestOptions(path: ApiConstants.refresh),
      ));

      await Future.delayed(const Duration(milliseconds: 50));

      // Refresh API should be called exactly once
      verify(() => mockRefreshDio.post(ApiConstants.refresh,
          data: any(named: 'data'))).called(1);

      // Both handlers should have their requests resolved (retried successfully)
      verify(() => errorHandler1.resolve(any())).called(1);
      verify(() => errorHandler2.resolve(any())).called(1);
    });

    test('null onForceLogout does not crash', () async {
      final interceptorNoCallback = AuthInterceptor(
        tokenStorage: tokenStorage,
        refreshDio: mockRefreshDio,
        onForceLogout: null,
      );

      when(() => mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => null);
      when(() => mockStorage.delete(key: any(named: 'key')))
          .thenAnswer((_) async {});

      final err = DioException(
        type: DioExceptionType.badResponse,
        response: Response(
          statusCode: 401,
          requestOptions: RequestOptions(path: '/api/weather'),
        ),
        requestOptions: RequestOptions(path: '/api/weather'),
      );

      interceptorNoCallback.onError(err, errorHandler);

      await Future.delayed(const Duration(milliseconds: 50));

      verify(() => errorHandler.next(err)).called(1);
    });
  });
}
