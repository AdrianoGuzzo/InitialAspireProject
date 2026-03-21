import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';

import '../../../auth/presentation/widgets/password_field.dart';

class ChangePasswordForm extends StatefulWidget {
  final Future<void> Function(String currentPassword, String newPassword)
      onSubmit;

  const ChangePasswordForm({super.key, required this.onSubmit});

  @override
  State<ChangePasswordForm> createState() => _ChangePasswordFormState();
}

class _ChangePasswordFormState extends State<ChangePasswordForm> {
  final _formKey = GlobalKey<FormState>();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  bool _isSaving = false;

  @override
  void dispose() {
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _isSaving = true);
    await widget.onSubmit(
      _currentPasswordController.text,
      _newPasswordController.text,
    );
    if (mounted) {
      setState(() => _isSaving = false);
      _currentPasswordController.clear();
      _newPasswordController.clear();
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;

    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            l.changePassword,
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 12),
          PasswordField(
            controller: _currentPasswordController,
            label: l.currentPassword,
            textInputAction: TextInputAction.next,
            validator: (v) {
              if (v == null || v.isEmpty) return l.fieldRequired;
              return null;
            },
          ),
          const SizedBox(height: 12),
          PasswordField(
            controller: _newPasswordController,
            label: l.newPassword,
            validator: (v) {
              if (v == null || v.isEmpty) return l.fieldRequired;
              if (v.length < 6) return l.passwordTooShort;
              return null;
            },
          ),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: _isSaving ? null : _submit,
            child: _isSaving
                ? const SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(l.changePassword),
          ),
        ],
      ),
    );
  }
}
