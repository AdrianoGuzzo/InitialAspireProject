import 'package:dio/dio.dart';

import '../../../../core/constants/api_constants.dart';
import '../../../../core/error/error_handler.dart';
import '../../../../core/error/result.dart';
import '../../domain/entities/weather_forecast.dart';
import '../../domain/repositories/weather_repository.dart';
import '../models/weather_forecast_dto.dart';

class WeatherRepositoryImpl implements WeatherRepository {
  final Dio _dio;

  WeatherRepositoryImpl(this._dio);

  @override
  Future<Result<List<WeatherForecast>>> getForecasts() async {
    try {
      final response = await _dio.get(ApiConstants.weather);
      final list = (response.data as List)
          .map((e) => WeatherForecastDto.fromJson(e as Map<String, dynamic>))
          .map((dto) => dto.toDomain())
          .toList();
      return Result.success(list);
    } catch (e) {
      return Result.failure(ErrorHandler.handle(e));
    }
  }
}
