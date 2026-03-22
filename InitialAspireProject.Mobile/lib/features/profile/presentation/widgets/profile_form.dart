import 'package:flutter/material.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../domain/entities/user_profile.dart';

class ProfileForm extends StatefulWidget {
  final UserProfile profile;
  final Future<void> Function(String fullName) onSave;

  const ProfileForm({
    super.key,
    required this.profile,
    required this.onSave,
  });

  @override
  State<ProfileForm> createState() => _ProfileFormState();
}

class _ProfileFormState extends State<ProfileForm> {
  late final TextEditingController _nameController;
  final _formKey = GlobalKey<FormState>();
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.profile.fullName);
  }

  @override
  void didUpdateWidget(ProfileForm oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.profile.fullName != widget.profile.fullName) {
      _nameController.text = widget.profile.fullName;
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _isSaving = true);
    await widget.onSave(_nameController.text.trim());
    if (mounted) setState(() => _isSaving = false);
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;

    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: _nameController,
            decoration: InputDecoration(
              labelText: l.fullName,
              border: const OutlineInputBorder(),
            ),
            validator: (v) {
              if (v == null || v.trim().isEmpty) return l.fieldRequired;
              return null;
            },
          ),
          const SizedBox(height: 8),
          TextFormField(
            initialValue: widget.profile.email,
            decoration: InputDecoration(
              labelText: l.email,
              border: const OutlineInputBorder(),
            ),
            enabled: false,
          ),
          if (widget.profile.roles.isNotEmpty) ...[
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              children: widget.profile.roles
                  .map((r) => Chip(label: Text(r)))
                  .toList(),
            ),
          ],
          const SizedBox(height: 16),
          FilledButton(
            onPressed: _isSaving ? null : _submit,
            child: _isSaving
                ? const SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(l.save),
          ),
        ],
      ),
    );
  }
}
