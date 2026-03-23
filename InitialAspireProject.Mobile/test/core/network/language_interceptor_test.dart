import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/network/language_interceptor.dart';
import 'package:mocktail/mocktail.dart';

class MockRequestInterceptorHandler extends Mock
    implements RequestInterceptorHandler {}

void main() {
  late LanguageInterceptor interceptor;
  late MockRequestInterceptorHandler handler;

  setUpAll(() {
    registerFallbackValue(RequestOptions(path: ''));
  });

  setUp(() {
    interceptor = LanguageInterceptor();
    handler = MockRequestInterceptorHandler();
  });

  group('onRequest', () {
    test('sets Accept-Language header from platform locale', () {
      RequestOptions? captured;
      when(() => handler.next(any())).thenAnswer((invocation) {
        captured = invocation.positionalArguments.first as RequestOptions;
      });

      final options = RequestOptions(path: '/api/test');
      interceptor.onRequest(options, handler);

      expect(captured, isNotNull);
      expect(captured!.headers['Accept-Language'], isNotNull);
      expect(captured!.headers['Accept-Language'], isNotEmpty);
    });

    test('calls handler.next with the options', () {
      final options = RequestOptions(path: '/api/test');
      interceptor.onRequest(options, handler);

      verify(() => handler.next(options)).called(1);
    });
  });
}
