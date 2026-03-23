import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/network/error_interceptor.dart';
import 'package:mocktail/mocktail.dart';

class MockErrorInterceptorHandler extends Mock
    implements ErrorInterceptorHandler {}

class MockResponseInterceptorHandler extends Mock
    implements ResponseInterceptorHandler {}

void main() {
  late ErrorInterceptor interceptor;

  setUp(() {
    interceptor = ErrorInterceptor();
  });

  group('onError', () {
    late MockErrorInterceptorHandler handler;

    setUp(() {
      handler = MockErrorInterceptorHandler();
    });

    test('wraps non-empty string response data into a map', () {
      final response = Response<dynamic>(
        statusCode: 500,
        data: 'Internal Server Error',
        requestOptions: RequestOptions(path: '/test'),
      );
      final err = DioException(
        type: DioExceptionType.badResponse,
        response: response,
        requestOptions: response.requestOptions,
      );

      interceptor.onError(err, handler);

      expect(err.response?.data, {'message': 'Internal Server Error'});
      verify(() => handler.next(err)).called(1);
    });

    test('does not wrap empty string data', () {
      final response = Response<dynamic>(
        statusCode: 500,
        data: '',
        requestOptions: RequestOptions(path: '/test'),
      );
      final err = DioException(
        type: DioExceptionType.badResponse,
        response: response,
        requestOptions: response.requestOptions,
      );

      interceptor.onError(err, handler);

      expect(err.response?.data, '');
      verify(() => handler.next(err)).called(1);
    });

    test('leaves map data unchanged', () {
      final response = Response<dynamic>(
        statusCode: 400,
        data: {'message': 'Bad request'},
        requestOptions: RequestOptions(path: '/test'),
      );
      final err = DioException(
        type: DioExceptionType.badResponse,
        response: response,
        requestOptions: response.requestOptions,
      );

      interceptor.onError(err, handler);

      expect(err.response?.data, {'message': 'Bad request'});
      verify(() => handler.next(err)).called(1);
    });

    test('passes through when response is null', () {
      final err = DioException(
        type: DioExceptionType.connectionError,
        requestOptions: RequestOptions(path: '/test'),
      );

      interceptor.onError(err, handler);

      verify(() => handler.next(err)).called(1);
    });
  });

  group('onResponse', () {
    test('passes response through', () {
      final handler = MockResponseInterceptorHandler();
      final response = Response(
        statusCode: 200,
        data: {'result': 'ok'},
        requestOptions: RequestOptions(path: '/test'),
      );

      interceptor.onResponse(response, handler);

      verify(() => handler.next(response)).called(1);
    });
  });
}
