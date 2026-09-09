import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:socar_dispatch_mobile/features/onboarding/data/onboarding_repository.dart';
import 'package:socar_dispatch_mobile/features/onboarding/presentation/bloc/onboarding_bloc.dart';
import 'package:socar_dispatch_mobile/features/onboarding/presentation/bloc/onboarding_event.dart';
import 'package:socar_dispatch_mobile/features/onboarding/presentation/bloc/onboarding_state.dart';
import 'package:socar_dispatch_mobile/features/onboarding/services/onboarding_permission_service.dart';

class MockOnboardingPermissionService extends OnboardingPermissionService {
  final OnboardingPermissionResult permissionResult;
  bool wasRequested = false;

  MockOnboardingPermissionService({
    this.permissionResult = OnboardingPermissionResult.granted,
  });

  @override
  Future<OnboardingPermissionResult> requestOnboardingPermissions() async {
    wasRequested = true;
    return permissionResult;
  }

  @override
  Future<bool> openSettings() async => true;
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('OnboardingBloc Unit Tests', () {
    test(
      'emits OnboardingRequired when consent has not been granted yet',
      () async {
        SharedPreferences.setMockInitialValues({});
        final prefs = await SharedPreferences.getInstance();
        final repository = OnboardingRepository(prefs: prefs);
        final permissionService = MockOnboardingPermissionService();

        final bloc = OnboardingBloc(
          onboardingRepository: repository,
          permissionService: permissionService,
        );

        expectLater(
          bloc.stream,
          emitsInOrder([const OnboardingLoading(), const OnboardingRequired()]),
        );

        bloc.add(const OnboardingCheckRequested());
      },
    );

    test(
      'emits OnboardingCompleted when consent was previously saved',
      () async {
        SharedPreferences.setMockInitialValues({'has_accepted_kvkk': true});
        final prefs = await SharedPreferences.getInstance();
        final repository = OnboardingRepository(prefs: prefs);
        final permissionService = MockOnboardingPermissionService();

        final bloc = OnboardingBloc(
          onboardingRepository: repository,
          permissionService: permissionService,
        );

        expectLater(
          bloc.stream,
          emitsInOrder([
            const OnboardingLoading(),
            const OnboardingCompleted(),
          ]),
        );

        bloc.add(const OnboardingCheckRequested());
      },
    );

    test('KvkkConsentToggled toggles isConsentChecked flag', () async {
      SharedPreferences.setMockInitialValues({});
      final prefs = await SharedPreferences.getInstance();
      final repository = OnboardingRepository(prefs: prefs);
      final permissionService = MockOnboardingPermissionService();

      final bloc = OnboardingBloc(
        onboardingRepository: repository,
        permissionService: permissionService,
      );

      bloc.add(const OnboardingCheckRequested());
      await pumpEventQueue();

      expectLater(
        bloc.stream,
        emits(const OnboardingRequired(isConsentChecked: true)),
      );

      bloc.add(const KvkkConsentToggled(true));
    });

    test(
      'OnboardingPermissionsRequested completes onboarding when permissions granted',
      () async {
        SharedPreferences.setMockInitialValues({});
        final prefs = await SharedPreferences.getInstance();
        final repository = OnboardingRepository(prefs: prefs);
        final permissionService = MockOnboardingPermissionService(
          permissionResult: OnboardingPermissionResult.granted,
        );

        final bloc = OnboardingBloc(
          onboardingRepository: repository,
          permissionService: permissionService,
        );

        bloc.add(const OnboardingCheckRequested());
        await pumpEventQueue();
        bloc.add(const KvkkConsentToggled(true));
        await pumpEventQueue();

        expectLater(
          bloc.stream,
          emitsInOrder([
            const OnboardingRequired(
              isConsentChecked: true,
              isSubmitting: true,
            ),
            const OnboardingCompleted(),
          ]),
        );

        bloc.add(const OnboardingPermissionsRequested());
        await pumpEventQueue();

        expect(permissionService.wasRequested, isTrue);
        expect(repository.hasAcceptedKvkk(), isTrue);
      },
    );
  });
}
