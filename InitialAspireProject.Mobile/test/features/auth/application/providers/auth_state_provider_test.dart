import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/error/failures.dart';
import 'package:initial_aspire_project_mobile/core/error/result.dart';
import 'package:initial_aspire_project_mobile/core/storage/secure_storage_provider.dart';
import 'package:initial_aspire_project_mobile/core/storage/token_storage.dart';
import 'package:initial_aspire_project_mobile/features/auth/application/providers/auth_providers.dart';
import 'package:initial_aspire_project_mobile/features/auth/application/providers/auth_state_provider.dart';
import 'package:initial_aspire_project_mobile/features/auth/domain/entities/auth_tokens.dart';
import 'package:initial_aspire_project_mobile/features/auth/domain/repositories/auth_repository.dart';
import 'package:mocktail/mocktail.dart';

class MockAuthRepository extends Mock implements AuthRepository {}

class MockTokenStorage extends Mock implements TokenStorage {}

/// Creates a minimal JWT with a given expiry timestamp.
/// The token has the format: header.payload.signature
String createTestJwt({required int exp}) {
  final header = base64Url.encode(utf8.encode('{"alg":"HS256","typ":"JWT"}'));
  final payload = base64Url.encode(utf8.encode('{"exp":$exp}'));
  return '$header.$payload.test-signature';
}

String get validJwt => createTestJwt(
      exp: DateTime.now().add(const Duration(hours: 1)).millisecondsSinceEpoch ~/ 1000,
    );

String get expiredJwt => createTestJwt(
      exp: DateTime.now().subtract(const Duration(hours: 1)).millisecondsSinceEpoch ~/ 1000,
    );

void main() {
  late MockAuthRepository mockRepo;
  late MockTokenStorage mockTokenStorage;

  setUp(() {
    mockRepo = MockAuthRepository();
    mockTokenStorage = MockTokenStorage();
  });

  ProviderContainer createContainer() {
    final container = ProviderContainer(overrides: [
      authRepositoryProvider.overrideWithValue(mockRepo),
      tokenStorageProvider.overrideWithValue(mockTokenStorage),
    ]);
    addTearDown(container.dispose);
    return container;
  }

  group('_init', () {
    test('no access token sets unauthenticated', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => null);

      final container = createContainer();
      // Read the provider to trigger _init
      container.read(authStateProvider);

      // Wait for async _init to complete
      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.unauthenticated);
    });

    test('valid non-expired token sets authenticated', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => validJwt);

      final container = createContainer();
      container.read(authStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.authenticated);
    });

    test('expired token with no refresh token sets unauthenticated', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => expiredJwt);
      when(() => mockTokenStorage.readRefreshToken())
          .thenAnswer((_) async => null);
      when(() => mockTokenStorage.clear()).thenAnswer((_) async {});

      final container = createContainer();
      container.read(authStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.unauthenticated);
      verify(() => mockTokenStorage.clear()).called(1);
    });

    test('expired token with successful refresh sets authenticated', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => expiredJwt);
      when(() => mockTokenStorage.readRefreshToken())
          .thenAnswer((_) async => 'old-rt');
      when(() => mockRepo.refresh(refreshToken: 'old-rt')).thenAnswer(
        (_) async => const Result.success(
          AuthTokens(accessToken: 'new-jwt', refreshToken: 'new-rt'),
        ),
      );
      when(() => mockTokenStorage.writeTokens(
            accessToken: 'new-jwt',
            refreshToken: 'new-rt',
          )).thenAnswer((_) async {});

      final container = createContainer();
      container.read(authStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.authenticated);
      verify(() => mockTokenStorage.writeTokens(
            accessToken: 'new-jwt',
            refreshToken: 'new-rt',
          )).called(1);
    });

    test('expired token with failed refresh sets unauthenticated', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => expiredJwt);
      when(() => mockTokenStorage.readRefreshToken())
          .thenAnswer((_) async => 'old-rt');
      when(() => mockRepo.refresh(refreshToken: 'old-rt')).thenAnswer(
        (_) async => const Result.failure(Failure.network()),
      );
      when(() => mockTokenStorage.clear()).thenAnswer((_) async {});

      final container = createContainer();
      container.read(authStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.unauthenticated);
      verify(() => mockTokenStorage.clear()).called(1);
    });
  });

  group('login', () {
    test('successful login sets authenticated and writes tokens', () async {
      // _init returns unauthenticated
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => null);
      when(() => mockRepo.login(
            email: 'test@test.com',
            password: 'pass',
          )).thenAnswer(
        (_) async => const Result.success(
          AuthTokens(accessToken: 'jwt', refreshToken: 'rt'),
        ),
      );
      when(() => mockTokenStorage.writeTokens(
            accessToken: 'jwt',
            refreshToken: 'rt',
          )).thenAnswer((_) async {});

      final container = createContainer();
      container.read(authStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      await container.read(authStateProvider.notifier).login(
            email: 'test@test.com',
            password: 'pass',
          );

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.authenticated);
      expect(state.failure, isNull);
      verify(() => mockTokenStorage.writeTokens(
            accessToken: 'jwt',
            refreshToken: 'rt',
          )).called(1);
    });

    test('failed login sets unauthenticated with failure', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => null);
      when(() => mockRepo.login(
            email: 'test@test.com',
            password: 'wrong',
          )).thenAnswer(
        (_) async => const Result.failure(
          Failure.server(message: 'Invalid credentials'),
        ),
      );

      final container = createContainer();
      container.read(authStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      await container.read(authStateProvider.notifier).login(
            email: 'test@test.com',
            password: 'wrong',
          );

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.unauthenticated);
      expect(state.failure, isA<ServerFailure>());
    });
  });

  group('logout', () {
    test('with refresh token revokes and clears', () async {
      // Start authenticated
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => validJwt);

      final container = createContainer();
      container.read(authStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      when(() => mockTokenStorage.readRefreshToken())
          .thenAnswer((_) async => 'rt');
      when(() => mockRepo.revoke(refreshToken: 'rt'))
          .thenAnswer((_) async => const Result.success(null));
      when(() => mockTokenStorage.clear()).thenAnswer((_) async {});

      await container.read(authStateProvider.notifier).logout();

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.unauthenticated);
      verify(() => mockRepo.revoke(refreshToken: 'rt')).called(1);
      verify(() => mockTokenStorage.clear()).called(1);
    });

    test('without refresh token skips revoke', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => validJwt);

      final container = createContainer();
      container.read(authStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      when(() => mockTokenStorage.readRefreshToken())
          .thenAnswer((_) async => null);
      when(() => mockTokenStorage.clear()).thenAnswer((_) async {});

      await container.read(authStateProvider.notifier).logout();

      final state = container.read(authStateProvider);
      expect(state.status, AuthStatus.unauthenticated);
      verifyNever(() => mockRepo.revoke(refreshToken: any(named: 'refreshToken')));
      verify(() => mockTokenStorage.clear()).called(1);
    });
  });

  group('clearFailure', () {
    test('clears failure from state', () async {
      when(() => mockTokenStorage.readAccessToken())
          .thenAnswer((_) async => null);
      when(() => mockRepo.login(
            email: 'test@test.com',
            password: 'wrong',
          )).thenAnswer(
        (_) async => const Result.failure(Failure.network()),
      );

      final container = createContainer();
      container.read(authStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      await container.read(authStateProvider.notifier).login(
            email: 'test@test.com',
            password: 'wrong',
          );

      expect(container.read(authStateProvider).failure, isNotNull);

      container.read(authStateProvider.notifier).clearFailure();

      expect(container.read(authStateProvider).failure, isNull);
    });
  });
}
