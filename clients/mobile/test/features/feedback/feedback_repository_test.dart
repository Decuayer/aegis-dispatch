import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/models/media_attachment_model.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/feedback_repository.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/models/create_feedback_request.dart';
import 'package:socar_dispatch_mobile/features/feedback/data/models/feedback_category.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/create_incident_request_model.dart';

class MockSecureStorageService extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'test-auth-token';
}

class MockHttpClientAdapter implements HttpClientAdapter {
  final Future<ResponseBody> Function(RequestOptions options) handler;

  MockHttpClientAdapter(this.handler);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) {
    return handler(options);
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  late Directory tempDir;
  late File samplePhoto;

  setUp(() async {
    tempDir = await Directory.systemTemp.createTemp('feedback_repo_test_');
    samplePhoto = File('${tempDir.path}/sample.jpg')
      ..writeAsBytesSync(List.filled(1024 * 10, 0)); // 10 KB
  });

  tearDown(() async {
    if (await tempDir.exists()) {
      await tempDir.delete(recursive: true);
    }
  });

  group('FeedbackRepository Unit Tests', () {
    test(
      'submitFeedback posts multipart/form-data and parses 201 Created successfully',
      () async {
        final storage = MockSecureStorageService();
        final apiClient = ApiClient(storageService: storage);

        apiClient.dio.httpClientAdapter = MockHttpClientAdapter((
          options,
        ) async {
          expect(options.path, contains('/api/v1/feedbacks'));
          expect(options.method, 'POST');
          expect(options.contentType, contains('multipart/form-data'));

          final responseData = {
            'success': true,
            'message': 'Feedback created successfully.',
            'data': {
              'id': 'fb-999',
              'userId': 'user-111',
              'userFullName': 'Ali Veli',
              'userEmail': 'ali@socar.az',
              'title': '[Bug Report] Valve malfunction',
              'description': 'Main valve pressure leaking in unit 4.',
              'status': 'Pending',
              'createdAt': '2026-09-03T10:00:00Z',
              'mediaAttachments': [
                {
                  'id': 'media-1',
                  'feedbackId': 'fb-999',
                  'mediaUrl': 'https://minio.socar.local/feedbacks/sample.jpg',
                  'mediaType': 'image/jpeg',
                  'createdAt': '2026-09-03T10:00:00Z',
                },
              ],
            },
          };

          return ResponseBody.fromString(
            jsonEncode(responseData),
            201,
            headers: {
              Headers.contentTypeHeader: [Headers.jsonContentType],
            },
          );
        });

        final repository = FeedbackRepository(apiClient: apiClient);
        final request = CreateFeedbackRequest(
          title: 'Valve malfunction',
          description: 'Main valve pressure leaking in unit 4.',
          category: FeedbackCategory.bugReport,
          attachments: [
            SelectedMediaFile(
              file: samplePhoto,
              mediaType: IncidentMediaType.photo,
              fileSizeBytes: 10 * 1024,
              fileName: 'sample.jpg',
            ),
          ],
        );

        final result = await repository.submitFeedback(request);

        expect(result.id, 'fb-999');
        expect(result.title, '[Bug Report] Valve malfunction');
        expect(result.mediaAttachments.length, 1);
        expect(result.mediaAttachments.first.mediaUrl, contains('sample.jpg'));
      },
    );

    test('submitFeedback throws formatted error on 400 bad request', () async {
      final storage = MockSecureStorageService();
      final apiClient = ApiClient(storageService: storage);

      apiClient.dio.httpClientAdapter = MockHttpClientAdapter((options) async {
        final errorPayload = {
          'success': false,
          'message': 'Validation failed',
          'errors': ['Title must not exceed 200 characters.'],
        };

        return ResponseBody.fromString(
          jsonEncode(errorPayload),
          400,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );
      });

      final repository = FeedbackRepository(apiClient: apiClient);
      final request = const CreateFeedbackRequest(
        title: 'Invalid',
        description: 'Desc',
      );

      expect(
        () => repository.submitFeedback(request),
        throwsA(
          predicate(
            (e) =>
                e.toString().contains('Title must not exceed 200 characters.'),
          ),
        ),
      );
    });
  });
}
