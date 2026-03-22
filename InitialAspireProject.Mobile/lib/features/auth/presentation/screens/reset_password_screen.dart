import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../../../core/error/failures.dart';
import '../../../../core/error/result.dart';
import '../../application/providers/auth_providers.dart';
import '../widgets/password_field.dart';

class ResetPasswordScreen extends ConsumerStatefulWidget {
  final String email;
  final String token;

  const ResetPasswordScreen({
    super.key,
    required this.email,
    required this.token,
  });

  @override
  ConsumerState<ResetPasswordScreen> createState() =>
      _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isLoading = true);

    final result = await ref.read(authRepositoryProvider).resetPassword(
          email: widget.email,
          token: widget.token,
          newPassword: _passwordController.text,
          confirmPassword: _confirmPasswordController.text,
        );

    if (!mounted) return;
    setState(() => _isLoading = false);

    final l = AppLocalizations.of(context)!;

    switch (result) {
      case Success():
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l.resetPasswordSuccess)),
        );
        context.go('/login');
      case ResultFailure(failure: final f):
        final message = switch (f) {
          ValidationFailure(:final errors) =>
            errors.values.expand((e) => e).join('\n'),
          NetworkFailure() => l.errorNetwork,
          ServerFailure(:final message) => message ?? l.errorServer,
          _ => l.errorUnknown,
        };
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(message)),
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(title: Text(l.resetPassword)),
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
                  PasswordField(
                    controller: _passwordController,
                    label: l.newPassword,
                    textInputAction: TextInputAction.next,
                    validator: (v) {
                      if (v == null || v.isEmpty) return l.fieldRequired;
                      if (v.length < 6) return l.passwordTooShort;
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  PasswordField(
                    controller: _confirmPasswordController,
                    label: l.confirmPassword,
                    validator: (v) {
                      if (v == null || v.isEmpty) return l.fieldRequired;
                      if (v != _passwordController.text) {
                        return l.passwordsDoNotMatch;
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: _isLoading ? null : _submit,
                    child: _isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text(l.resetPassword),
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
