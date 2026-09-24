import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/auth/data/models/auth_response_model.dart';
import 'package:socar_dispatch_mobile/features/auth/data/models/register_request_model.dart';
import 'package:socar_dispatch_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:socar_dispatch_mobile/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:socar_dispatch_mobile/features/auth/presentation/bloc/auth_event.dart';
import 'package:socar_dispatch_mobile/features/auth/presentation/bloc/auth_state.dart';
import 'package:socar_dispatch_mobile/features/auth/presentation/views/register_view.dart';
import 'package:socar_dispatch_mobile/features/profile/data/models/user_model.dart';

class MockSecureStorageService extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'dummy-jwt-token';
  @override
  Future<void> saveAccessToken(String token) async {}
  @override
  Future<void> saveUserData(UserModel user) async {}
  @override
  Future<UserModel?> getUserData() async => null;
  @override
  Future<void> clearSession() async {}
}

class FakeAuthRepository extends AuthRepository {
  FakeAuthRepository()
    : super(
        apiClient: ApiClient(storageService: MockSecureStorageService()),
        storageService: MockSecureStorageService(),
      );

  bool shouldFail = false;
  String errorMessage = 'Registration error';
  RegisterRequestModel? lastRequest;

  @override
  Future<AuthResponseModel> register(RegisterRequestModel request) async {
    lastRequest = request;
    if (shouldFail) {
      throw Exception(errorMessage);
    }
    return AuthResponseModel(
      accessToken: 'sample-jwt-token',
      expiresAt: DateTime.now().add(const Duration(days: 7)),
      user: UserModel(
        id: 'new-emp-id',
        firstName: request.firstName,
        lastName: request.lastName,
        email: request.email,
        phone: request.phone,
        department: request.department,
        roleType: RoleType.employee,
      ),
    );
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('RegisterRequestModel Tests', () {
    test('toJson produces correct payload structure', () {
      const model = RegisterRequestModel(
        firstName: 'Nigar',
        lastName: 'Gasimova',
        email: 'nigar@socar.az',
        password: 'Password123!',
        phone: '+994501234567',
        department: 'Operations',
      );

      final json = model.toJson();

      expect(json['firstName'], 'Nigar');
      expect(json['lastName'], 'Gasimova');
      expect(json['email'], 'nigar@socar.az');
      expect(json['password'], 'Password123!');
      expect(json['phone'], '+994501234567');
      expect(json['department'], 'Operations');
      expect(json['roleType'], 0);
    });
  });

  group('AuthBloc Register Flow Tests', () {
    late FakeAuthRepository repository;
    late AuthBloc authBloc;

    setUp(() {
      repository = FakeAuthRepository();
      authBloc = AuthBloc(authRepository: repository);
    });

    tearDown(() {
      authBloc.close();
    });

    test(
      'AuthRegisterRequested emits [AuthLoading, Authenticated] on success',
      () async {
        const request = RegisterRequestModel(
          firstName: 'Ali',
          lastName: 'Mammadov',
          email: 'ali@socar.az',
          password: 'SecretPassword',
          phone: '+994509998877',
          department: 'HSSE',
        );

        final expectedStates = [
          isA<AuthLoading>(),
          isA<Authenticated>().having(
            (s) => s.user.email,
            'email',
            'ali@socar.az',
          ),
        ];

        expectLater(authBloc.stream, emitsInOrder(expectedStates));

        authBloc.add(const AuthRegisterRequested(request));
      },
    );

    test(
      'AuthRegisterRequested emits [AuthLoading, AuthFailure] on error',
      () async {
        repository.shouldFail = true;
        repository.errorMessage = 'A user with this email already exists.';

        const request = RegisterRequestModel(
          firstName: 'Ali',
          lastName: 'Mammadov',
          email: 'existing@socar.az',
          password: 'SecretPassword',
          phone: '+994509998877',
          department: 'HSSE',
        );

        final expectedStates = [
          isA<AuthLoading>(),
          isA<AuthFailure>().having(
            (s) => s.errorMessage,
            'errorMessage',
            contains('already exists'),
          ),
        ];

        expectLater(authBloc.stream, emitsInOrder(expectedStates));

        authBloc.add(const AuthRegisterRequested(request));
      },
    );
  });

  group('RegisterView Widget Tests', () {
    late FakeAuthRepository fakeRepo;
    late AuthBloc authBloc;

    setUp(() {
      fakeRepo = FakeAuthRepository();
      authBloc = AuthBloc(authRepository: fakeRepo);
    });

    tearDown(() {
      authBloc.close();
    });

    testWidgets('renders all registration form fields and buttons', (
      tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: BlocProvider.value(
            value: authBloc,
            child: const RegisterView(),
          ),
        ),
      );

      expect(find.text('Employee Registration'), findsOneWidget);
      expect(find.text('First Name'), findsOneWidget);
      expect(find.text('Last Name'), findsOneWidget);
      expect(find.text('Corporate Email'), findsOneWidget);
      expect(find.text('Phone Number'), findsOneWidget);
      expect(find.text('Password'), findsOneWidget);
      expect(find.text('Confirm Password'), findsOneWidget);
      expect(find.text('Create Employee Account'), findsOneWidget);
      expect(find.text('Already have an account? '), findsOneWidget);
    });
  });
}
