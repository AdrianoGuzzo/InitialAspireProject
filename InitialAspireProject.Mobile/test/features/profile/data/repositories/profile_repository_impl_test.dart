import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:initial_aspire_project_mobile/core/constants/api_constants.dart';
import 'package:initial_aspire_project_mobile/core/error/failures.dart';
import 'package:initial_aspire_project_mobile/core/error/result.dart';
import 'package:initial_aspire_project_mobile/features/profile/data/repositories/profile_repository_impl.dart';
import 'package:initial_aspire_project_mobile/features/profile/domain/entities/user_profile.dart';
import 'package:mocktail/mocktail.dart';

class MockDio extends Mock implements Dio {}

void main() {
  late MockDio mockDio;
  late ProfileRepositoryImpl repository;

  setUp(() {
    mockDio = MockDio();
    repository = ProfileRepositoryImpl(mockDio);
  });

  group('getProfile', () {
    test('returns UserProfile on success', () async {
      when(() => mockDio.get(ApiConstants.profile)).thenAnswer(
        (_) async => Response(
          data: {
            'email': 'admin@localhost',
            'fullName': 'Admin',
            'roles': ['Admin', 'User'],
          },
          statusCode: 200,
          requestOptions: RequestOptions(path: ApiConstants.profile),
        ),
      );

      final result = await repository.getProfile();

      expect(result, isA<Success<UserProfile>>());
      final profile = (result as Success<UserProfile>).data;
      expect(profile.email, 'admin@localhost');
      expect(profile.fullName, 'Admin');
      expect(profile.roles, ['Admin', 'User']);
    });

    test('returns failure on error', () async {
      when(() => mockDio.get(ApiConstants.profile)).thenThrow(
        DioException(
          type: DioExceptionType.badResponse,
          response: Response(
            statusCode: 401,
            requestOptions: RequestOptions(path: ApiConstants.profile),
          ),
          requestOptions: RequestOptions(path: ApiConstants.profile),
        ),
      );

      final result = await repository.getProfile();

      expect(result, isA<ResultFailure<UserProfile>>());
      final failure = (result as ResultFailure<UserProfile>).failure;
      expect(failure, isA<UnauthorizedFailure>());
    });
  });

  group('updateProfile', () {
    test('returns success on 200', () async {
      when(() => mockDio.put(ApiConstants.profile, data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions: RequestOptions(path: ApiConstants.profile),
              ));

      final result = await repository.updateProfile(fullName: 'New Name');

      expect(result, isA<Success<void>>());
    });
  });

  group('changePassword', () {
    test('returns success on 200', () async {
      when(() => mockDio.post(ApiConstants.changePassword,
              data: any(named: 'data')))
          .thenAnswer((_) async => Response(
                statusCode: 200,
                requestOptions:
                    RequestOptions(path: ApiConstants.changePassword),
              ));

      final result = await repository.changePassword(
        currentPassword: 'old',
        newPassword: 'new',
      );

      expect(result, isA<Success<void>>());
    });
  });
}
