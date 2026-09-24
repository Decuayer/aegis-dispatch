import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/models/media_attachment_model.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/feedback_repository.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/models/create_feedback_request.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/models/feedback_category.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/models/feedback_response_model.dart';
import 'package:socar_dispatch_mobile/features/feedback/presentation/bloc/feedback_bloc.dart';
import 'package:socar_dispatch_mobile/features/feedback/presentation/bloc/feedback_event.dart';
import 'package:socar_dispatch_mobile/features/feedback/presentation/bloc/feedback_state.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/create_incident_request_model.dart';

class FakeSecureStorage extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'test-token';
}

class FakeFeedbackRepository extends FeedbackRepository {
  FakeFeedbackRepository()
    : super(apiClient: ApiClient(storageService: FakeSecureStorage()));

  bool shouldFail = false;
  String errorMessage = 'Server unreachable';
  CreateFeedbackRequest? capturedRequest;

  @override
  Future<FeedbackResponseModel> submitFeedback(
    CreateFeedbackRequest request,
  ) async {
    capturedRequest = request;
    if (shouldFail) {
      throw Exception(errorMessage);
    }
    return FeedbackResponseModel(
      id: 'fb-success-123',
      userId: 'user-001',
      userFullName: 'Field Operator',
      userEmail: 'operator@socar.az',
      title: request.formattedTitle,
      description: request.description,
      status: 'Pending',
      createdAt: DateTime.now(),
    );
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late FakeFeedbackRepository repository;
  late FeedbackBloc bloc;
  late Directory tempDir;
  late File sampleFile;

  setUp(() async {
    repository = FakeFeedbackRepository();
    bloc = FeedbackBloc(repository: repository);
    tempDir = await Directory.systemTemp.createTemp('bloc_test_');
    sampleFile = File('${tempDir.path}/test.png')..writeAsBytesSync([0, 1, 2]);
  });

  tearDown(() async {
    await bloc.close();
    if (await tempDir.exists()) {
      await tempDir.delete(recursive: true);
    }
  });

  SelectedMediaFile createTestMedia(String name) {
    return SelectedMediaFile(
      file: sampleFile,
      mediaType: IncidentMediaType.photo,
      fileSizeBytes: 1024,
      fileName: name,
    );
  }

  group('FeedbackBloc Unit Tests', () {
    test('initial state is FeedbackInitial with defaults', () {
      expect(bloc.state, isA<FeedbackInitial>());
      expect(bloc.state.title, isEmpty);
      expect(bloc.state.description, isEmpty);
      expect(bloc.state.category, FeedbackCategory.bugReport);
      expect(bloc.state.attachments, isEmpty);
    });

    test(
      'FeedbackTitleChanged, DescriptionChanged, and CategoryChanged update draft',
      () async {
        bloc.add(const FeedbackTitleChanged('Pump issue'));
        bloc.add(const FeedbackDescriptionChanged('Pressure dropping rapidly'));
        bloc.add(
          const FeedbackCategoryChanged(FeedbackCategory.operationalIssue),
        );
        await pumpEventQueue();

        expect(bloc.state.title, 'Pump issue');
        expect(bloc.state.description, 'Pressure dropping rapidly');
        expect(bloc.state.category, FeedbackCategory.operationalIssue);
      },
    );

    test(
      'FeedbackMediaAdded and FeedbackMediaRemoved update attachments correctly',
      () async {
        final media1 = createTestMedia('photo1.png');
        final media2 = createTestMedia('photo2.png');

        bloc.add(FeedbackMediaAdded([media1, media2]));
        await pumpEventQueue();
        expect(bloc.state.attachments.length, 2);

        bloc.add(const FeedbackMediaRemoved(0));
        await pumpEventQueue();
        expect(bloc.state.attachments.length, 1);
        expect(bloc.state.attachments.first.fileName, 'photo2.png');
      },
    );

    test(
      'FeedbackMediaAdded rejects exceeding 5 attachments limit with FeedbackFailure',
      () async {
        final items = List.generate(6, (i) => createTestMedia('photo$i.png'));

        bloc.add(FeedbackMediaAdded(items));
        await pumpEventQueue();

        expect(bloc.state, isA<FeedbackFailure>());
        expect(
          (bloc.state as FeedbackFailure).errorMessage,
          contains('Maximum 5 media attachments allowed'),
        );
      },
    );

    test('FeedbackSubmitted validates empty title and description', () async {
      bloc.add(const FeedbackSubmitted());
      await pumpEventQueue();
      expect(bloc.state, isA<FeedbackFailure>());
      expect(
        (bloc.state as FeedbackFailure).errorMessage,
        'Title is required.',
      );

      bloc.add(const FeedbackTitleChanged('Valid Title'));
      bloc.add(const FeedbackSubmitted());
      await pumpEventQueue();
      expect(bloc.state, isA<FeedbackFailure>());
      expect(
        (bloc.state as FeedbackFailure).errorMessage,
        'Description is required.',
      );
    });

    test(
      'FeedbackSubmitted emits FeedbackSubmitting and FeedbackSuccess on success',
      () async {
        bloc.add(const FeedbackTitleChanged('Valve failure'));
        bloc.add(
          const FeedbackDescriptionChanged(
            'Valve 4 is stuck in closed position.',
          ),
        );
        bloc.add(
          const FeedbackCategoryChanged(FeedbackCategory.operationalIssue),
        );
        await pumpEventQueue();

        expectLater(
          bloc.stream,
          emitsInOrder([isA<FeedbackSubmitting>(), isA<FeedbackSuccess>()]),
        );

        bloc.add(const FeedbackSubmitted());
      },
    );

    test(
      'FeedbackSubmitted emits FeedbackFailure and preserves draft on error',
      () async {
        repository.shouldFail = true;
        repository.errorMessage = 'Network connection failed.';

        bloc.add(const FeedbackTitleChanged('Cooling leak'));
        bloc.add(
          const FeedbackDescriptionChanged('Cooling pipe leaking water.'),
        );
        await pumpEventQueue();

        expectLater(
          bloc.stream,
          emitsInOrder([isA<FeedbackSubmitting>(), isA<FeedbackFailure>()]),
        );

        bloc.add(const FeedbackSubmitted());
        await pumpEventQueue();

        expect(bloc.state.title, 'Cooling leak');
        expect(bloc.state.description, 'Cooling pipe leaking water.');
      },
    );
  });
}
