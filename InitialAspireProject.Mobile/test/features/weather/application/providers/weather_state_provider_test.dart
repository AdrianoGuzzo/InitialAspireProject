import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/error/failures.dart';
import 'package:initial_aspire_project_mobile/core/error/result.dart';
import 'package:initial_aspire_project_mobile/features/weather/application/providers/weather_providers.dart';
import 'package:initial_aspire_project_mobile/features/weather/application/providers/weather_state_provider.dart';
import 'package:initial_aspire_project_mobile/features/weather/domain/entities/weather_forecast.dart';
import 'package:initial_aspire_project_mobile/features/weather/domain/repositories/weather_repository.dart';
import 'package:mocktail/mocktail.dart';

class MockWeatherRepository extends Mock implements WeatherRepository {}

void main() {
  late MockWeatherRepository mockRepo;

  setUp(() {
    mockRepo = MockWeatherRepository();
  });

  ProviderContainer createContainer() {
    final container = ProviderContainer(overrides: [
      weatherRepositoryProvider.overrideWithValue(mockRepo),
    ]);
    addTearDown(container.dispose);
    return container;
  }

  group('load', () {
    test('success sets forecasts in state', () async {
      final forecasts = [
        WeatherForecast(
          date: DateTime(2026, 3, 22),
          temperatureC: 25,
          temperatureF: 77,
          summary: 'Warm',
        ),
        WeatherForecast(
          date: DateTime(2026, 3, 23),
          temperatureC: 18,
          temperatureF: 64,
          summary: 'Cool',
        ),
      ];
      when(() => mockRepo.getForecasts())
          .thenAnswer((_) async => Result.success(forecasts));

      final container = createContainer();
      container.read(weatherStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(weatherStateProvider);
      expect(state.forecasts, hasLength(2));
      expect(state.forecasts.first.summary, 'Warm');
      expect(state.isLoading, isFalse);
      expect(state.failure, isNull);
    });

    test('failure sets failure in state', () async {
      when(() => mockRepo.getForecasts()).thenAnswer(
        (_) async => const Result.failure(Failure.network()),
      );

      final container = createContainer();
      container.read(weatherStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(weatherStateProvider);
      expect(state.forecasts, isEmpty);
      expect(state.failure, isA<NetworkFailure>());
    });

    test('empty list on success sets empty forecasts', () async {
      when(() => mockRepo.getForecasts())
          .thenAnswer((_) async => const Result.success([]));

      final container = createContainer();
      container.read(weatherStateProvider);

      await Future.delayed(const Duration(milliseconds: 50));

      final state = container.read(weatherStateProvider);
      expect(state.forecasts, isEmpty);
      expect(state.failure, isNull);
    });
  });
}
