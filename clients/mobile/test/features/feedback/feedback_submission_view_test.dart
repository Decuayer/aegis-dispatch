import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/feedback_repository.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/models/create_feedback_request.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/models/feedback_response_model.dart';
import 'package:socar_dispatch_mobile/features/feedback/presentation/views/feedback_submission_view.dart';
import 'package:socar_dispatch_mobile/features/feedback/presentation/widgets/feedback_success_dialog.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/media_picker_service.dart';

class StubSecureStorage extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'stub-token';
}

class StubFeedbackRepository extends FeedbackRepository {
  StubFeedbackRepository() : super(apiClient: ApiClient(storageService: StubSecureStorage()));

  bool shouldFail = false;

  @override
  Future<FeedbackResponseModel> submitFeedback(CreateFeedbackRequest request) async {
    if (shouldFail) {
      throw Exception('Submission failed from server');
    }
    return FeedbackResponseModel(
      id: 'mock-fb-12345678',
      userId: 'user-1',
      userFullName: 'Test User',
      userEmail: 'test@socar.az',
      title: request.formattedTitle,
      description: request.description,
      status: 'Pending',
      createdAt: DateTime.now(),
    );
  }
}

class StubMediaPickerService extends MediaPickerService {}

void main() {
  Widget createWidget({
    FeedbackRepository? repository,
    MediaPickerService? pickerService,
  }) {
    return MaterialApp(
      home: FeedbackSubmissionView(
        feedbackRepository: repository ?? StubFeedbackRepository(),
        mediaPickerService: pickerService ?? StubMediaPickerService(),
      ),
    );
  }

  group('FeedbackSubmissionView Widget Tests', () {
    testWidgets('renders all form elements, category chips, and submit button', (tester) async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      expect(find.text('Send Feedback'), findsOneWidget);
      expect(find.text('Feedback Category'), findsOneWidget);
      expect(find.text('Bug Report'), findsOneWidget);
      expect(find.text('Improvement Suggestion'), findsOneWidget);
      expect(find.text('Operational Issue'), findsOneWidget);
      expect(find.text('Title *'), findsOneWidget);
      expect(find.text('Description *'), findsOneWidget);
      expect(find.text('Media Attachments'), findsOneWidget);
      expect(find.text('Submit Feedback'), findsOneWidget);
    });

    testWidgets('shows validation errors when submitting with empty fields', (tester) async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final submitBtn = find.text('Submit Feedback');
      await tester.ensureVisible(submitBtn);
      await tester.tap(submitBtn);
      await tester.pumpAndSettle();

      expect(find.text('Title is required.'), findsOneWidget);
      expect(find.text('Description is required.'), findsOneWidget);
    });

    testWidgets('switching categories updates choice chip selection', (tester) async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final chip = find.text('Operational Issue');
      await tester.tap(chip);
      await tester.pumpAndSettle();

      final ChoiceChip choiceChipWidget = tester.widget(
        find.ancestor(of: chip, matching: find.byType(ChoiceChip)),
      );
      expect(choiceChipWidget.selected, isTrue);
    });

    testWidgets('submitting valid feedback displays FeedbackSuccessDialog', (tester) async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextFormField).first, 'Generator noise');
      await tester.enterText(find.byType(TextFormField).last, 'Excessive vibrations and noise during peak operation.');
      await tester.pumpAndSettle();

      final submitBtn = find.text('Submit Feedback');
      await tester.ensureVisible(submitBtn);
      await tester.tap(submitBtn);
      await tester.pumpAndSettle();

      expect(find.byType(FeedbackSuccessDialog), findsOneWidget);
      expect(find.text('Feedback Submitted'), findsOneWidget);
      expect(find.text('Done / Return'), findsOneWidget);
    });
  });
}
