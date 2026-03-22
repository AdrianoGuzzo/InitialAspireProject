import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/application/providers/auth_state_provider.dart';
import '../config/env_config.dart';
import '../storage/secure_storage_provider.dart';
import 'auth_interceptor.dart';
import 'error_interceptor.dart';
import 'language_interceptor.dart';

/// A raw Dio instance with no auth interceptor, used for refresh calls
/// to avoid infinite loops.
final _refreshDioProvider = Provider<Dio>((ref) {
  return Dio(BaseOptions(
    baseUrl: EnvConfig.baseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 15),
    contentType: 'application/json',
  ));
});

/// The main Dio instance with all interceptors attached.
final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(BaseOptions(
    baseUrl: EnvConfig.baseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 15),
    contentType: 'application/json',
  ));

  final tokenStorage = ref.watch(tokenStorageProvider);
  final refreshDio = ref.watch(_refreshDioProvider);

  dio.interceptors.addAll([
    LanguageInterceptor(),
    AuthInterceptor(
      tokenStorage: tokenStorage,
      refreshDio: refreshDio,
      onForceLogout: () {
        ref.read(authStateProvider.notifier).logout();
      },
    ),
    ErrorInterceptor(),
  ]);

  return dio;
});
