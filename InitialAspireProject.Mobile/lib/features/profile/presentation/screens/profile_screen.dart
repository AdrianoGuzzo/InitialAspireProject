import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../../../core/error/failures.dart';
import '../../../../core/error/result.dart';
import '../../application/providers/profile_state_provider.dart';
import '../widgets/change_password_form.dart';
import '../widgets/profile_form.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context)!;
    final state = ref.watch(profileStateProvider);

    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.failure != null) {
      final message = switch (state.failure!) {
        NetworkFailure() => l.errorNetwork,
        UnauthorizedFailure() => l.errorUnauthorized,
        ServerFailure(:final message) => message ?? l.errorServer,
        _ => l.errorUnknown,
      };

      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(message),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: () =>
                  ref.read(profileStateProvider.notifier).load(),
              icon: const Icon(Icons.refresh),
              label: Text(l.retry),
            ),
          ],
        ),
      );
    }

    final profile = state.profile;
    if (profile == null) {
      return const Center(child: CircularProgressIndicator());
    }

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          ProfileForm(
            profile: profile,
            onSave: (fullName) async {
              final result = await ref
                  .read(profileStateProvider.notifier)
                  .updateProfile(fullName: fullName);
              if (context.mounted) {
                switch (result) {
                  case Success():
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text(l.profileUpdateSuccess)),
                    );
                  case ResultFailure(failure: final f):
                    final msg = switch (f) {
                      ValidationFailure(:final errors) =>
                        errors.values.expand((e) => e).join('\n'),
                      ServerFailure(:final message) =>
                        message ?? l.errorServer,
                      _ => l.errorUnknown,
                    };
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text(msg)),
                    );
                }
              }
            },
          ),
          const SizedBox(height: 32),
          const Divider(),
          const SizedBox(height: 16),
          ChangePasswordForm(
            onSubmit: (currentPassword, newPassword) async {
              final result = await ref
                  .read(profileStateProvider.notifier)
                  .changePassword(
                    currentPassword: currentPassword,
                    newPassword: newPassword,
                  );
              if (context.mounted) {
                switch (result) {
                  case Success():
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text(l.changePasswordSuccess)),
                    );
                  case ResultFailure(failure: final f):
                    final msg = switch (f) {
                      ValidationFailure(:final errors) =>
                        errors.values.expand((e) => e).join('\n'),
                      ServerFailure(:final message) =>
                        message ?? l.errorServer,
                      _ => l.errorUnknown,
                    };
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text(msg)),
                    );
                }
              }
            },
          ),
        ],
      ),
    );
  }
}
