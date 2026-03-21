import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/error/failures.dart';
import '../../../../core/error/result.dart';
import '../../domain/entities/weather_forecast.dart';
import 'weather_providers.dart';

class WeatherState {
  final List<WeatherForecast> forecasts;
  final bool isLoading;
  final Failure? failure;

  const WeatherState({
    this.forecasts = const [],
    this.isLoading = false,
    this.failure,
  });

  WeatherState copyWith({
    List<WeatherForecast>? forecasts,
    bool? isLoading,
    Failure? failure,
  }) {
    return WeatherState(
      forecasts: forecasts ?? this.forecasts,
      isLoading: isLoading ?? this.isLoading,
      failure: failure,
    );
  }
}

class WeatherStateNotifier extends StateNotifier<WeatherState> {
  final Ref _ref;

  WeatherStateNotifier(this._ref) : super(const WeatherState()) {
    load();
  }

  Future<void> load() async {
    state = state.copyWith(isLoading: true, failure: null);

    final result = await _ref.read(weatherRepositoryProvider).getForecasts();

    switch (result) {
      case Success(data: final data):
        state = WeatherState(forecasts: data);
      case ResultFailure(failure: final f):
        state = WeatherState(failure: f);
    }
  }
}

final weatherStateProvider =
    StateNotifierProvider<WeatherStateNotifier, WeatherState>((ref) {
  return WeatherStateNotifier(ref);
});
