import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:socar_dispatch_mobile/features/onboarding/data/onboarding_repository.dart';
import 'package:socar_dispatch_mobile/features/onboarding/presentation/bloc/onboarding_bloc.dart';
import 'package:socar_dispatch_mobile/features/onboarding/presentation/bloc/onboarding_event.dart';
import 'package:socar_dispatch_mobile/features/onboarding/presentation/views/kvkk_consent_view.dart';
import 'package:socar_dispatch_mobile/features/onboarding/services/onboarding_permission_service.dart';

class MockPermissionService extends OnboardingPermissionService {
  @override
  Future<OnboardingPermissionResult> requestOnboardingPermissions() async {
    return OnboardingPermissionResult.granted;
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('KvkkConsentView Widget Tests', () {
    Widget buildTestWidget(OnboardingBloc bloc) {
      return MaterialApp(
        home: BlocProvider<OnboardingBloc>.value(
          value: bloc,
          child: const KvkkConsentView(),
        ),
      );
    }

    testWidgets(
      'continue button is disabled until consent checkbox is checked',
      (tester) async {
        SharedPreferences.setMockInitialValues({});
        final prefs = await SharedPreferences.getInstance();
        final repository = OnboardingRepository(prefs: prefs);
        final permissionService = MockPermissionService();
        final bloc = OnboardingBloc(
          onboardingRepository: repository,
          permissionService: permissionService,
        )..add(const OnboardingCheckRequested());

        await tester.pumpWidget(buildTestWidget(bloc));
        await tester.pumpAndSettle();

        final buttonFinder = find.widgetWithText(
          ElevatedButton,
          'Continue & Grant Permissions',
        );
        expect(buttonFinder, findsOneWidget);

        final button = tester.widget<ElevatedButton>(buttonFinder);
        expect(button.enabled, isFalse);

        final checkboxFinder = find.byType(Checkbox);
        expect(checkboxFinder, findsOneWidget);
        await tester.tap(checkboxFinder);
        await tester.pumpAndSettle();

        final enabledButton = tester.widget<ElevatedButton>(buttonFinder);
        expect(enabledButton.enabled, isTrue);

        bloc.close();
      },
    );

    testWidgets(
      'tapping enabled continue button triggers permission flow and persists consent',
      (tester) async {
        SharedPreferences.setMockInitialValues({});
        final prefs = await SharedPreferences.getInstance();
        final repository = OnboardingRepository(prefs: prefs);
        final permissionService = MockPermissionService();
        final bloc = OnboardingBloc(
          onboardingRepository: repository,
          permissionService: permissionService,
        )..add(const OnboardingCheckRequested());

        await tester.pumpWidget(buildTestWidget(bloc));
        await tester.pumpAndSettle();

        await tester.tap(find.byType(Checkbox));
        await tester.pumpAndSettle();

        await tester.tap(
          find.widgetWithText(ElevatedButton, 'Continue & Grant Permissions'),
        );
        await tester.pumpAndSettle();

        expect(repository.hasAcceptedKvkk(), isTrue);

        bloc.close();
      },
    );
  });
}
