import 'dart:async';

import 'package:dio/dio.dart';

import '../constants/api_constants.dart';
import '../storage/token_storage.dart';

/// Callback invoked when a forced logout is needed (refresh failed on 401).
typedef ForceLogoutCallback = void Function();

class AuthInterceptor extends Interceptor {
  final TokenStorage _tokenStorage;
  final Dio _refreshDio;
  final ForceLogoutCallback? _onForceLogout;

  Completer<bool>? _refreshCompleter;

  AuthInterceptor({
    required TokenStorage tokenStorage,
    required Dio refreshDio,
    ForceLogoutCallback? onForceLogout,
  })  : _tokenStorage = tokenStorage,
        _refreshDio = refreshDio,
        _onForceLogout = onForceLogout;

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final path = options.path;
    final isPublic = ApiConstants.publicPaths.any((p) => path.contains(p));

    if (!isPublic) {
      final token = await _tokenStorage.readAccessToken();
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }

    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode != 401) {
      return handler.next(err);
    }

    // Don't retry if already retried
    if (err.requestOptions.extra['retried'] == true) {
      return handler.next(err);
    }

    // Don't retry auth endpoints
    final path = err.requestOptions.path;
    if (path.contains(ApiConstants.refresh) ||
        path.contains(ApiConstants.login)) {
      return handler.next(err);
    }

    final didRefresh = await _tryRefresh();

    if (!didRefresh) {
      await _tokenStorage.clear();
      _onForceLogout?.call();
      return handler.next(err);
    }

    // Retry the original request with the new token
    try {
      final token = await _tokenStorage.readAccessToken();
      final opts = err.requestOptions;
      opts.headers['Authorization'] = 'Bearer $token';
      opts.extra['retried'] = true;

      final response = await _refreshDio.fetch(opts);
      return handler.resolve(response);
    } on DioException catch (retryError) {
      return handler.next(retryError);
    }
  }

  Future<bool> _tryRefresh() async {
    // If a refresh is already in progress, wait for it
    if (_refreshCompleter != null) {
      return _refreshCompleter!.future;
    }

    _refreshCompleter = Completer<bool>();

    try {
      final refreshToken = await _tokenStorage.readRefreshToken();
      if (refreshToken == null) {
        _refreshCompleter!.complete(false);
        return false;
      }

      final response = await _refreshDio.post(
        ApiConstants.refresh,
        data: {'refreshToken': refreshToken},
      );

      if (response.statusCode == 200 && response.data is Map) {
        final data = response.data as Map<String, dynamic>;
        await _tokenStorage.writeTokens(
          accessToken: data['token'] as String,
          refreshToken: data['refreshToken'] as String,
        );
        _refreshCompleter!.complete(true);
        return true;
      }

      _refreshCompleter!.complete(false);
      return false;
    } catch (_) {
      _refreshCompleter!.complete(false);
      return false;
    } finally {
      _refreshCompleter = null;
    }
  }
}
