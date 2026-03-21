import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:initial_aspire_project_mobile/l10n/app_localizations.dart';

import '../../domain/entities/weather_forecast.dart';

class WeatherCard extends StatelessWidget {
  final WeatherForecast forecast;

  const WeatherCard({super.key, required this.forecast});

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final dateStr = DateFormat.yMMMd().format(forecast.date);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: ListTile(
        leading: const Icon(Icons.wb_sunny, size: 36),
        title: Text(dateStr),
        subtitle: Text(forecast.summary),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              '${forecast.temperatureC}°C',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            Text(
              '${forecast.temperatureF}°F',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ),
      ),
    );
  }
}
