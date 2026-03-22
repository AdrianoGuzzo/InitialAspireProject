import 'dart:ui';

import 'package:dio/dio.dart';

class LanguageInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final locale = PlatformDispatcher.instance.locale;
    final languageTag = locale.toLanguageTag(); // e.g. "pt-BR", "en", "es"
    options.headers['Accept-Language'] = languageTag;
    handler.next(options);
  }
}
