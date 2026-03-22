import 'package:freezed_annotation/freezed_annotation.dart';

part 'weather_forecast.freezed.dart';

@freezed
class WeatherForecast with _$WeatherForecast {
  const factory WeatherForecast({
    required DateTime date,
    required int temperatureC,
    required int temperatureF,
    required String summary,
  }) = _WeatherForecast;
}
