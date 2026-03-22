import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../../../core/error/failures.dart';
import '../../../../core/error/result.dart';
import '../../application/providers/auth_providers.dart';

class ConfirmEmailScreen extends ConsumerStatefulWidget {
  final String email;
  final String token;

  const ConfirmEmailScreen({
    super.key,
    required this.email,
    required this.token,
  });

  @override
  ConsumerState<ConfirmEmailScreen> createState() =>
      _ConfirmEmailScreenState();
}

class _ConfirmEmailScreenState extends ConsumerState<ConfirmEmailScreen> {
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _confirm();
  }

  Future<void> _confirm() async {
    final result = await ref.read(authRepositoryProvider).confirmEmail(
          email: widget.email,
          token: widget.token,
        );

    if (!mounted) return;

    final l = AppLocalizations.of(context)!;

    switch (result) {
      case Success():
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l.confirmEmailSuccess)),
        );
        // Navigate to login after a brief delay
        Future.delayed(const Duration(seconds: 2), () {
          if (mounted) context.go('/login');
        });
      case ResultFailure(failure: final f):
        setState(() {
          _isLoading = false;
          _errorMessage = switch (f) {
            ServerFailure(:final message) => message ?? l.errorServer,
            NetworkFailure() => l.errorNetwork,
            _ => l.errorUnknown,
          };
        });
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(title: Text(l.confirmEmail)),
      body: Center(
        child: _isLoading
            ? const CircularProgressIndicator()
            : _errorMessage != null
                ? Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.error_outline,
                          size: 64, color: Colors.red),
                      const SizedBox(height: 16),
                      Text(_errorMessage!,
                          textAlign: TextAlign.center),
                      const SizedBox(height: 24),
                      FilledButton(
                        onPressed: () => context.go('/login'),
                        child: Text(l.login),
                      ),
                    ],
                  )
                : Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.check_circle_outline,
                          size: 64, color: Colors.green),
                      const SizedBox(height: 16),
                      Text(l.confirmEmailSuccess),
                    ],
                  ),
      ),
    );
  }
}
