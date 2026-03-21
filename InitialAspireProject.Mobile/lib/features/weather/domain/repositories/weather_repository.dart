import '../../../../core/error/result.dart';
import '../entities/weather_forecast.dart';

abstract class WeatherRepository {
  Future<Result<List<WeatherForecast>>> getForecasts();
}
