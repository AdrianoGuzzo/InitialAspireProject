import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/constants/storage_keys.dart';
import 'package:initial_aspire_project_mobile/core/storage/token_storage.dart';
import 'package:mocktail/mocktail.dart';

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}

void main() {
  late MockFlutterSecureStorage mockStorage;
  late TokenStorage tokenStorage;

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    tokenStorage = TokenStorage(mockStorage);
  });

  group('TokenStorage', () {
    test('readAccessToken delegates to secure storage', () async {
      when(() => mockStorage.read(key: StorageKeys.accessToken))
          .thenAnswer((_) async => 'test-token');

      final result = await tokenStorage.readAccessToken();
      expect(result, 'test-token');
    });

    test('readRefreshToken delegates to secure storage', () async {
      when(() => mockStorage.read(key: StorageKeys.refreshToken))
          .thenAnswer((_) async => 'refresh-token');

      final result = await tokenStorage.readRefreshToken();
      expect(result, 'refresh-token');
    });

    test('writeTokens writes both tokens', () async {
      when(() => mockStorage.write(key: any(named: 'key'), value: any(named: 'value')))
          .thenAnswer((_) async {});

      await tokenStorage.writeTokens(
        accessToken: 'access',
        refreshToken: 'refresh',
      );

      verify(() => mockStorage.write(
            key: StorageKeys.accessToken,
            value: 'access',
          )).called(1);
      verify(() => mockStorage.write(
            key: StorageKeys.refreshToken,
            value: 'refresh',
          )).called(1);
    });

    test('clear deletes both tokens', () async {
      when(() => mockStorage.delete(key: any(named: 'key')))
          .thenAnswer((_) async {});

      await tokenStorage.clear();

      verify(() => mockStorage.delete(key: StorageKeys.accessToken)).called(1);
      verify(() => mockStorage.delete(key: StorageKeys.refreshToken)).called(1);
    });
  });
}
