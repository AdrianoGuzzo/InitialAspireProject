import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/constants/api_constants.dart';
import 'package:initial_aspire_project_mobile/core/error/failures.dart';
import 'package:initial_aspire_project_mobile/core/error/result.dart';
import 'package:initial_aspire_project_mobile/features/weather/data/repositories/weather_repository_impl.dart';
import 'package:initial_aspire_project_mobile/features/weather/domain/entities/weather_forecast.dart';
import 'package:mocktail/mocktail.dart';

class MockDio extends Mock implements Dio {}

void main() {
  late MockDio mockDio;
  late WeatherRepositoryImpl repository;

  setUp(() {
    mockDio = MockDio();
    repository = WeatherRepositoryImpl(mockDio);
  });

  group('getForecasts', () {
    test('returns list of forecasts on success', () async {
      when(() => mockDio.get(ApiConstants.weather)).thenAnswer(
        (_) async => Response(
          data: [
            {
              'date': '2026-03-21',
              'temperatureC': 25,
              'temperatureF': 77,
              'summary': 'Warm',
            },
            {
              'date': '2026-03-22',
              'temperatureC': 18,
              'temperatureF': 64,
              'summary': 'Cool',
            },
          ],
          statusCode: 200,
          requestOptions: RequestOptions(path: ApiConstants.weather),
        ),
      );

      final result = await repository.getForecasts();

      expect(result, isA<Success<List<WeatherForecast>>>());
      final forecasts = (result as Success<List<WeatherForecast>>).data;
      expect(forecasts.length, 2);
      expect(forecasts[0].temperatureC, 25);
      expect(forecasts[0].summary, 'Warm');
      expect(forecasts[1].temperatureC, 18);
    });

    test('returns failure on error', () async {
      when(() => mockDio.get(ApiConstants.weather)).thenThrow(
        DioException(
          type: DioExceptionType.connectionError,
          requestOptions: RequestOptions(path: ApiConstants.weather),
        ),
      );

      final result = await repository.getForecasts();

      expect(result, isA<ResultFailure<List<WeatherForecast>>>());
      final failure =
          (result as ResultFailure<List<WeatherForecast>>).failure;
      expect(failure, isA<NetworkFailure>());
    });
  });
}
