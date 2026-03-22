import 'package:flutter/material.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../core/error/failures.dart';

void showErrorSnackbar(BuildContext context, Failure failure) {
  final l = AppLocalizations.of(context)!;

  final message = switch (failure) {
    ServerFailure(:final message) => message ?? l.errorServer,
    NetworkFailure() => l.errorNetwork,
    UnauthorizedFailure() => l.errorUnauthorized,
    ValidationFailure(:final errors) =>
      errors.values.expand((e) => e).join('\n'),
    EmailNotConfirmedFailure(:final message) =>
      message ?? l.errorEmailNotConfirmed,
    UnknownFailure(:final message) => message ?? l.errorUnknown,
  };

  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text(message)),
  );
}
