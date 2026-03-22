import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../../../core/error/failures.dart';
import '../../application/providers/auth_providers.dart';
import '../../application/providers/auth_state_provider.dart';
import '../widgets/auth_text_field.dart';
import '../widgets/password_field.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    await ref.read(authStateProvider.notifier).login(
          email: _emailController.text.trim(),
          password: _passwordController.text,
        );
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final authState = ref.watch(authStateProvider);

    ref.listen<AuthState>(authStateProvider, (prev, next) {
      if (next.failure != null) {
        final message = switch (next.failure!) {
          EmailNotConfirmedFailure(:final message) =>
            message ?? l.errorEmailNotConfirmed,
          NetworkFailure() => l.errorNetwork,
          ServerFailure(:final message) => message ?? l.errorServer,
          ValidationFailure(:final errors) =>
            errors.values.expand((e) => e).join('\n'),
          UnauthorizedFailure() => l.errorUnauthorized,
          UnknownFailure(:final message) => message ?? l.errorUnknown,
        };
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(message)),
        );
        ref.read(authStateProvider.notifier).clearFailure();
      }
    });

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Form(
              key: _formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    l.login,
                    style: Theme.of(context).textTheme.headlineMedium,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 32),
                  AuthTextField(
                    controller: _emailController,
                    label: l.email,
                    keyboardType: TextInputType.emailAddress,
                    autofocus: true,
                    validator: (v) {
                      if (v == null || v.trim().isEmpty) return l.fieldRequired;
                      if (!v.contains('@')) return l.invalidEmail;
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  PasswordField(
                    controller: _passwordController,
                    label: l.password,
                    validator: (v) {
                      if (v == null || v.isEmpty) return l.fieldRequired;
                      return null;
                    },
                  ),
                  const SizedBox(height: 8),
                  Align(
                    alignment: Alignment.centerRight,
                    child: TextButton(
                      onPressed: () => context.go('/forgot-password'),
                      child: Text(l.forgotPassword),
                    ),
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: authState.isLoading ? null : _submit,
                    child: authState.isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text(l.login),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(l.noAccount),
                      TextButton(
                        onPressed: () => context.go('/register'),
                        child: Text(l.register),
                      ),
                    ],
                  ),
                  // Show resend activation if email not confirmed
                  if (authState.failure is EmailNotConfirmedFailure)
                    TextButton(
                      onPressed: () async {
                        final email = _emailController.text.trim();
                        if (email.isNotEmpty) {
                          final authRepo = ref.read(
                            authRepositoryProvider,
                          );
                          await authRepo.resendActivation(email: email);
                          if (context.mounted) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text(l.resendActivationSuccess)),
                            );
                          }
                        }
                      },
                      child: Text(l.resendActivation),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
