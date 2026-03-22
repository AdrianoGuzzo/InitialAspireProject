import 'package:dio/dio.dart';

/// Normalizes plain-string error responses into JSON maps so the rest of the
/// pipeline can always assume `response.data` is a Map when there is an error.
class ErrorInterceptor extends Interceptor {
  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final data = err.response?.data;
    if (data is String && data.isNotEmpty) {
      err.response?.data = {'message': data};
    }
    handler.next(err);
  }
}
