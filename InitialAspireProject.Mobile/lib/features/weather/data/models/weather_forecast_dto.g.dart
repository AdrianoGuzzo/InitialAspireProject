// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'weather_forecast_dto.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

WeatherForecastDto _$WeatherForecastDtoFromJson(Map<String, dynamic> json) =>
    WeatherForecastDto(
      date: json['date'] as String,
      temperatureC: (json['temperatureC'] as num).toInt(),
      temperatureF: (json['temperatureF'] as num).toInt(),
      summary: json['summary'] as String?,
    );
