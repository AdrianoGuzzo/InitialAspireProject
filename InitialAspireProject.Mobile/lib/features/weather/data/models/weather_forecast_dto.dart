import 'package:json_annotation/json_annotation.dart';

import '../../domain/entities/weather_forecast.dart';

part 'weather_forecast_dto.g.dart';

@JsonSerializable(createToJson: false)
class WeatherForecastDto {
  final String date;
  final int temperatureC;
  final int temperatureF;
  final String? summary;

  const WeatherForecastDto({
    required this.date,
    required this.temperatureC,
    required this.temperatureF,
    this.summary,
  });

  factory WeatherForecastDto.fromJson(Map<String, dynamic> json) =>
      _$WeatherForecastDtoFromJson(json);

  WeatherForecast toDomain() => WeatherForecast(
        date: DateTime.parse(date),
        temperatureC: temperatureC,
        temperatureF: temperatureF,
        summary: summary ?? '',
      );
}
