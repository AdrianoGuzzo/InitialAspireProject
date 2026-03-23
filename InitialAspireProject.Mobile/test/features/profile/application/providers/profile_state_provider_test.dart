import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/error/failures.dart';
import 'package:initial_aspire_project_mobile/core/error/result.dart';
import 'package:initial_aspire_project_mobile/features/profile/application/providers/profile_providers.dart';
import 'package:initial_aspire_project_mobile/features/profile/application/providers/profile_state_provider.dart';
import 'package:initial_aspire_project_mobile/features/profile/domain/entities/user_profile.dart';
import 'package:initial_aspire_project_mobile/features/profile/domain/repositories/profile_repository.dart';
import 'package:mocktail/mocktail.dart';

class MockProfileRepository extends Mock implements ProfileRepository {}

void main() {
  late MockProfileRepository mockRepo;

  setUp(() {
    mockRepo = MockProfileRepository();
  });

  ProviderContainer createContainer() {
    final container = ProviderContainer(overrides: [
      profileRepositoryProvider.overrideWithValue(mockRepo),
    ]);
    addTearDown(container.dispose);
    return container;
  }

  group('load', () {
    test('success sets profile in state', () async {
      const profile = UserProfile(
        email: 'admin@localhost',
        fullName: 'Admin',
        roles: ['Admin'],
      );
      when(() => mockRepo.getProfile())
          .thenAnswer((_) async => const Result.success(profile));

      final container = createContainer();
      container.read(profileStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(profileStateProvider);
      expect(state.profile, profile);
      expect(state.isLoading, isFalse);
      expect(state.failure, isNull);
    });

    test('failure sets failure in state', () async {
      when(() => mockRepo.getProfile()).thenAnswer(
        (_) async => const Result.failure(Failure.unauthorized()),
      );

      final container = createContainer();
      container.read(profileStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(profileStateProvider);
      expect(state.profile, isNull);
      expect(state.failure, isA<UnauthorizedFailure>());
    });
  });

  group('updateProfile', () {
    test('success reloads profile', () async {
      const profile = UserProfile(
        email: 'admin@localhost',
        fullName: 'Admin',
        roles: ['Admin'],
      );
      const updatedProfile = UserProfile(
        email: 'admin@localhost',
        fullName: 'New Name',
        roles: ['Admin'],
      );

      // First load
      when(() => mockRepo.getProfile())
          .thenAnswer((_) async => const Result.success(profile));

      final container = createContainer();
      container.read(profileStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      // Update
      when(() => mockRepo.updateProfile(fullName: 'New Name'))
          .thenAnswer((_) async => const Result.success(null));
      when(() => mockRepo.getProfile())
          .thenAnswer((_) async => const Result.success(updatedProfile));

      final result = await container
          .read(profileStateProvider.notifier)
          .updateProfile(fullName: 'New Name');

      expect(result, isA<Success<void>>());

      final state = container.read(profileStateProvider);
      expect(state.profile?.fullName, 'New Name');
      // getProfile called: once in constructor + once after update
      verify(() => mockRepo.getProfile()).called(2);
    });

    test('failure does not reload profile', () async {
      const profile = UserProfile(
        email: 'admin@localhost',
        fullName: 'Admin',
        roles: ['Admin'],
      );

      when(() => mockRepo.getProfile())
          .thenAnswer((_) async => const Result.success(profile));

      final container = createContainer();
      container.read(profileStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      when(() => mockRepo.updateProfile(fullName: ''))
          .thenAnswer((_) async => const Result.failure(
                Failure.validation(errors: {
                  'FullName': ['Required']
                }),
              ));

      final result = await container
          .read(profileStateProvider.notifier)
          .updateProfile(fullName: '');

      expect(result, isA<ResultFailure<void>>());
      // getProfile only called once (constructor), not again after failed update
      verify(() => mockRepo.getProfile()).called(1);
    });
  });

  group('changePassword', () {
    test('delegates to repo and returns success', () async {
      when(() => mockRepo.getProfile()).thenAnswer(
        (_) async => const Result.success(
          UserProfile(email: 'a@b.com', fullName: 'A', roles: []),
        ),
      );
      when(() => mockRepo.changePassword(
            currentPassword: 'old',
            newPassword: 'new',
          )).thenAnswer((_) async => const Result.success(null));

      final container = createContainer();
      container.read(profileStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      final result = await container
          .read(profileStateProvider.notifier)
          .changePassword(currentPassword: 'old', newPassword: 'new');

      expect(result, isA<Success<void>>());
    });

    test('delegates to repo and returns failure', () async {
      when(() => mockRepo.getProfile()).thenAnswer(
        (_) async => const Result.success(
          UserProfile(email: 'a@b.com', fullName: 'A', roles: []),
        ),
      );
      when(() => mockRepo.changePassword(
            currentPassword: 'wrong',
            newPassword: 'new',
          )).thenAnswer(
        (_) async => const Result.failure(
          Failure.validation(errors: {
            'CurrentPassword': ['Incorrect']
          }),
        ),
      );

      final container = createContainer();
      container.read(profileStateProvider);
      await Future.delayed(const Duration(milliseconds: 50));

      final result = await container
          .read(profileStateProvider.notifier)
          .changePassword(currentPassword: 'wrong', newPassword: 'new');

      expect(result, isA<ResultFailure<void>>());
    });
  });
}
