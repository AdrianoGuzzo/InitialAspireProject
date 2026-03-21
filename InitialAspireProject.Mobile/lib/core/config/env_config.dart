enum Environment { dev, staging, prod }

class EnvConfig {
  static const _env = String.fromEnvironment('ENV', defaultValue: 'dev');

  static Environment get environment {
    switch (_env) {
      case 'staging':
        return Environment.staging;
      case 'prod':
        return Environment.prod;
      default:
        return Environment.dev;
    }
  }

  static String get baseUrl {
    switch (environment) {
      case Environment.dev:
        return const String.fromEnvironment(
          'BASE_URL',
          defaultValue: 'https://localhost:7040',
        );
      case Environment.staging:
        return const String.fromEnvironment('BASE_URL');
      case Environment.prod:
        return const String.fromEnvironment('BASE_URL');
    }
  }
}
