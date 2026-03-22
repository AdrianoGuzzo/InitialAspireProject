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

void main() {
  late MockFlutterSecureStorage mockStorage;
  late TokenStorage tokenStorage;
  late MockDio mockRefreshDio;
  late AuthInterceptor interceptor;
  late MockRequestInterceptorHandler handler;

  setUpAll(() {
    registerFallbackValue(RequestOptions(path: ''));
  });

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    tokenStorage = TokenStorage(mockStorage);
    mockRefreshDio = MockDio();
    handler = MockRequestInterceptorHandler();

    interceptor = AuthInterceptor(
      tokenStorage: tokenStorage,
      refreshDio: mockRefreshDio,
      onForceLogout: () {},
    );
  });

  group('onRequest', () {
    test('adds Bearer token for non-public paths', () async {
      // Arrange
      when(() => mockStorage.read(key: any(named: 'key')))
          .thenAnswer((_) async => 'test-token');

      RequestOptions? capturedOptions;

      when(() => handler.next(any())).thenAnswer((invocation) {
        capturedOptions =
            invocation.positionalArguments.first as RequestOptions;
      });

      final options = RequestOptions(path: ApiConstants.weather);

      // Act
      interceptor.onRequest(options, handler);

      await Future.delayed(Duration.zero);

      // Assert
      verify(() => handler.next(any())).called(1);
      expect(capturedOptions, isNotNull);
      expect(
        capturedOptions!.headers['Authorization'],
        'Bearer test-token',
      );
    });

    test('does not add Bearer token for public paths', () async {
      // Arrange
      RequestOptions? capturedOptions;

      when(() => handler.next(any())).thenAnswer((invocation) {
        capturedOptions =
            invocation.positionalArguments.first as RequestOptions;
      });

      final options = RequestOptions(path: ApiConstants.login);

      // Act
      interceptor.onRequest(options, handler);

      await Future.delayed(Duration.zero);

      // Assert
      verify(() => handler.next(any())).called(1);
      expect(capturedOptions, isNotNull);
      expect(capturedOptions!.headers['Authorization'], isNull);
    });
  });
}
