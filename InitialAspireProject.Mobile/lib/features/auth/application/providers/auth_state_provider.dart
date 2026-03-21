import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

import '../../../../core/error/failures.dart';
import '../../../../core/error/result.dart';
import '../../../../core/storage/secure_storage_provider.dart';
import 'auth_providers.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthState {
  final AuthStatus status;
  final bool isLoading;
  final Failure? failure;

  const AuthState({
    this.status = AuthStatus.unknown,
    this.isLoading = false,
    this.failure,
  });

  AuthState copyWith({
    AuthStatus? status,
    bool? isLoading,
    Failure? failure,
  }) {
    return AuthState(
      status: status ?? this.status,
      isLoading: isLoading ?? this.isLoading,
      failure: failure,
    );
  }
}

class AuthStateNotifier extends StateNotifier<AuthState> {
  final Ref _ref;

  AuthStateNotifier(this._ref) : super(const AuthState()) {
    _init();
  }

  Future<void> _init() async {
    final tokenStorage = _ref.read(tokenStorageProvider);
    final accessToken = await tokenStorage.readAccessToken();

    if (accessToken == null) {
      state = const AuthState(status: AuthStatus.unauthenticated);
      return;
    }

    // Check if token is expired
    if (JwtDecoder.isExpired(accessToken)) {
      // Try to refresh
      final refreshToken = await tokenStorage.readRefreshToken();
      if (refreshToken == null) {
        await tokenStorage.clear();
        state = const AuthState(status: AuthStatus.unauthenticated);
        return;
      }

      final repo = _ref.read(authRepositoryProvider);
      final result = await repo.refresh(refreshToken: refreshToken);

      switch (result) {
        case Success(data: final tokens):
          await tokenStorage.writeTokens(
            accessToken: tokens.accessToken,
            refreshToken: tokens.refreshToken,
          );
          state = const AuthState(status: AuthStatus.authenticated);
        case ResultFailure():
          await tokenStorage.clear();
          state = const AuthState(status: AuthStatus.unauthenticated);
      }
      return;
    }

    state = const AuthState(status: AuthStatus.authenticated);
  }

  Future<void> login({
    required String email,
    required String password,
  }) async {
    state = state.copyWith(isLoading: true, failure: null);

    final repo = _ref.read(authRepositoryProvider);
    final result = await repo.login(email: email, password: password);

    switch (result) {
      case Success(data: final tokens):
        final tokenStorage = _ref.read(tokenStorageProvider);
        await tokenStorage.writeTokens(
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
        );
        state = const AuthState(status: AuthStatus.authenticated);
      case ResultFailure(failure: final f):
        state = AuthState(
          status: AuthStatus.unauthenticated,
          failure: f,
        );
    }
  }

  Future<void> logout() async {
    final tokenStorage = _ref.read(tokenStorageProvider);
    final refreshToken = await tokenStorage.readRefreshToken();

    // Best-effort revoke
    if (refreshToken != null) {
      final repo = _ref.read(authRepositoryProvider);
      await repo.revoke(refreshToken: refreshToken);
    }

    await tokenStorage.clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  void clearFailure() {
    state = state.copyWith(failure: null);
  }
}

final authStateProvider =
    StateNotifierProvider<AuthStateNotifier, AuthState>((ref) {
  return AuthStateNotifier(ref);
});

final isAuthenticatedProvider = Provider<bool>((ref) {
  return ref.watch(authStateProvider).status == AuthStatus.authenticated;
});

final authStatusProvider = Provider<AuthStatus>((ref) {
  return ref.watch(authStateProvider).status;
});
