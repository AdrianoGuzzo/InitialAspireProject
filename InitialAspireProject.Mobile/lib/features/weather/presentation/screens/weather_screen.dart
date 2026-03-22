import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../../../core/error/failures.dart';
import '../../application/providers/weather_state_provider.dart';
import '../widgets/weather_card.dart';

class WeatherScreen extends ConsumerWidget {
  const WeatherScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context)!;
    final state = ref.watch(weatherStateProvider);

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
            const Icon(Icons.cloud_off, size: 64),
            const SizedBox(height: 16),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: () =>
                  ref.read(weatherStateProvider.notifier).load(),
              icon: const Icon(Icons.refresh),
              label: Text(l.retry),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(weatherStateProvider.notifier).load(),
      child: ListView.builder(
        padding: const EdgeInsets.symmetric(vertical: 8),
        itemCount: state.forecasts.length,
        itemBuilder: (context, index) =>
            WeatherCard(forecast: state.forecasts[index]),
      ),
    );
  }
}
