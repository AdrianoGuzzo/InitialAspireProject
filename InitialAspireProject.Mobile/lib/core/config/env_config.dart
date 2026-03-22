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
    final url = switch (environment) {
      Environment.dev => const String.fromEnvironment(
        'BASE_URL',
        defaultValue: 'https://localhost:7040',
      ),
      Environment.staging => const String.fromEnvironment('BASE_URL'),
      Environment.prod => const String.fromEnvironment('BASE_URL'),
    };
    assert(url.isNotEmpty, 'BASE_URL must be set for $environment environment');
    return url;
  }
}
