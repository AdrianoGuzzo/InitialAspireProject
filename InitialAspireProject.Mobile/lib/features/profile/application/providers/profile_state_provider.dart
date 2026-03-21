import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/error/failures.dart';
import '../../../../core/error/result.dart';
import '../../domain/entities/user_profile.dart';
import 'profile_providers.dart';

class ProfileState {
  final UserProfile? profile;
  final bool isLoading;
  final Failure? failure;

  const ProfileState({
    this.profile,
    this.isLoading = false,
    this.failure,
  });

  ProfileState copyWith({
    UserProfile? profile,
    bool? isLoading,
    Failure? failure,
  }) {
    return ProfileState(
      profile: profile ?? this.profile,
      isLoading: isLoading ?? this.isLoading,
      failure: failure,
    );
  }
}

class ProfileStateNotifier extends StateNotifier<ProfileState> {
  final Ref _ref;

  ProfileStateNotifier(this._ref) : super(const ProfileState()) {
    load();
  }

  Future<void> load() async {
    state = state.copyWith(isLoading: true, failure: null);

    final result = await _ref.read(profileRepositoryProvider).getProfile();

    switch (result) {
      case Success(data: final data):
        state = ProfileState(profile: data);
      case ResultFailure(failure: final f):
        state = ProfileState(failure: f);
    }
  }

  Future<Result<void>> updateProfile({required String fullName}) async {
    final result = await _ref.read(profileRepositoryProvider).updateProfile(
          fullName: fullName,
        );

    switch (result) {
      case Success():
        // Reload profile to get updated data
        await load();
      case ResultFailure():
        break;
    }

    return result;
  }

  Future<Result<void>> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    return await _ref.read(profileRepositoryProvider).changePassword(
          currentPassword: currentPassword,
          newPassword: newPassword,
        );
  }
}

final profileStateProvider =
    StateNotifierProvider<ProfileStateNotifier, ProfileState>((ref) {
  return ProfileStateNotifier(ref);
});
