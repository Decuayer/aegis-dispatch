import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:socar_dispatch_mobile/features/onboarding/data/onboarding_repository.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('OnboardingRepository Unit Tests', () {
    test(
      'hasAcceptedKvkk returns false by default when preferences are empty',
      () async {
        SharedPreferences.setMockInitialValues({});
        final prefs = await SharedPreferences.getInstance();
        final repository = OnboardingRepository(prefs: prefs);

        expect(repository.hasAcceptedKvkk(), isFalse);
        expect(repository.getKvkkAcceptedAt(), isNull);
      },
    );

    test('hasAcceptedKvkk returns true when key is pre-configured', () async {
      SharedPreferences.setMockInitialValues({'has_accepted_kvkk': true});
      final prefs = await SharedPreferences.getInstance();
      final repository = OnboardingRepository(prefs: prefs);

      expect(repository.hasAcceptedKvkk(), isTrue);
    });

    test(
      'setKvkkAccepted records both boolean flag and ISO timestamp',
      () async {
        SharedPreferences.setMockInitialValues({});
        final prefs = await SharedPreferences.getInstance();
        final repository = OnboardingRepository(prefs: prefs);

        final success = await repository.setKvkkAccepted(true);
        expect(success, isTrue);
        expect(repository.hasAcceptedKvkk(), isTrue);
        expect(repository.getKvkkAcceptedAt(), isNotNull);

        // Verify clear functionality
        await repository.clearOnboarding();
        expect(repository.hasAcceptedKvkk(), isFalse);
        expect(repository.getKvkkAcceptedAt(), isNull);
      },
    );
  });
}
