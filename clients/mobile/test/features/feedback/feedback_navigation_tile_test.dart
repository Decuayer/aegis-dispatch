import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/feedback_repository.dart';
import 'package:socar_dispatch_mobile/features/feedback/presentation/views/feedback_submission_view.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/media_picker_service.dart';
import 'package:socar_dispatch_mobile/features/profile/presentation/widgets/feedback_navigation_tile.dart';

class StubSecureStorage extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'stub-token';
}

void main() {
  Widget createWidget() {
    final storage = StubSecureStorage();
    final apiClient = ApiClient(storageService: storage);
    final feedbackRepository = FeedbackRepository(apiClient: apiClient);
    final mediaPickerService = MediaPickerService();

    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider.value(value: feedbackRepository),
        RepositoryProvider.value(value: mediaPickerService),
      ],
      child: const MaterialApp(home: Scaffold(body: FeedbackNavigationTile())),
    );
  }

  group('FeedbackNavigationTile Widget Tests', () {
    testWidgets('renders feedback navigation tile text and icons accurately', (
      tester,
    ) async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      expect(find.text('Send Feedback / Report Issue'), findsOneWidget);
      expect(
        find.text(
          'Report bugs, operational challenges, or submit improvements.',
        ),
        findsOneWidget,
      );
      expect(find.byIcon(Icons.feedback_outlined), findsOneWidget);
      expect(find.byIcon(Icons.chevron_right_rounded), findsOneWidget);
    });

    testWidgets('tapping tile navigates to FeedbackSubmissionView', (
      tester,
    ) async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      await tester.tap(find.byType(FeedbackNavigationTile));
      await tester.pumpAndSettle();

      expect(find.byType(FeedbackSubmissionView), findsOneWidget);
      expect(find.text('Send Feedback'), findsOneWidget);
    });
  });
}
