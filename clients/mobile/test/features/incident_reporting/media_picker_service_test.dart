import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker/image_picker.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/create_incident_request_model.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/media_picker_service.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/thumbnail_generator_service.dart';

class FakeImagePicker extends Fake implements ImagePicker {
  XFile? returnedImage;
  XFile? returnedVideo;
  List<XFile> returnedMultiImages = [];
  List<XFile> returnedMultipleMedia = [];

  @override
  Future<XFile?> pickImage({
    required ImageSource source,
    double? maxWidth,
    double? maxHeight,
    int? imageQuality,
    CameraDevice preferredCameraDevice = CameraDevice.rear,
    bool requestFullMetadata = true,
  }) async => returnedImage;

  @override
  Future<XFile?> pickVideo({
    required ImageSource source,
    CameraDevice preferredCameraDevice = CameraDevice.rear,
    Duration? maxDuration,
  }) async => returnedVideo;

  @override
  Future<List<XFile>> pickMultiImage({
    double? maxWidth,
    double? maxHeight,
    int? imageQuality,
    int? limit,
    bool requestFullMetadata = true,
  }) async => returnedMultiImages;

  @override
  Future<List<XFile>> pickMultipleMedia({
    double? maxWidth,
    double? maxHeight,
    int? imageQuality,
    int? limit,
    bool requestFullMetadata = true,
  }) async => returnedMultipleMedia;
}

void main() {
  late FakeImagePicker fakePicker;
  late ThumbnailGeneratorService mockThumbnailService;
  late MediaPickerService service;
  late Directory tempDir;

  setUp(() async {
    tempDir = await Directory.systemTemp.createTemp('media_picker_test_');
    fakePicker = FakeImagePicker();
    mockThumbnailService = ThumbnailGeneratorService(
      thumbnailGenerator: ({
        required String video,
        String? thumbnailPath,
        dynamic imageFormat,
        int maxHeight = 0,
        int quality = 10,
      }) async => '${tempDir.path}/mock_thumb.jpg',
      tempDirectoryProvider: () async => tempDir,
    );

    service = MediaPickerService(
      picker: fakePicker,
      thumbnailService: mockThumbnailService,
    );
  });

  tearDown(() async {
    if (await tempDir.exists()) {
      await tempDir.delete(recursive: true);
    }
  });

  File createTestFile(String fileName, int sizeBytes) {
    final file = File('${tempDir.path}/$fileName');
    file.writeAsBytesSync(List.filled(sizeBytes, 0));
    return file;
  }

  group('MediaPickerService Unit Tests', () {
    test('successfully picks and processes valid photo under 10MB', () async {
      final file = createTestFile('evidence.jpg', 2 * 1024 * 1024); // 2 MB
      fakePicker.returnedImage = XFile(file.path);

      final result = await service.pickImageFromCamera();

      expect(result, isNotNull);
      expect(result!.fileName, 'evidence.jpg');
      expect(result.mediaType, IncidentMediaType.photo);
      expect(result.formattedFileSize, '2.0 MB');
    });

    test('throws FileQuotaExceededException when photo exceeds 10MB', () async {
      final file = createTestFile('large.png', 11 * 1024 * 1024); // 11 MB
      fakePicker.returnedImage = XFile(file.path);

      expect(
        () => service.pickImageFromCamera(),
        throwsA(isA<FileQuotaExceededException>()),
      );
    });

    test('throws UnsupportedMediaFormatException for rejected photo formats', () async {
      final file = createTestFile('invalid.bmp', 1024);
      fakePicker.returnedImage = XFile(file.path);

      expect(
        () => service.pickImageFromCamera(),
        throwsA(isA<UnsupportedMediaFormatException>()),
      );
    });

    test('successfully records video and generates thumbnail', () async {
      final file = createTestFile('incident.mp4', 15 * 1024 * 1024); // 15 MB
      fakePicker.returnedVideo = XFile(file.path);

      final result = await service.recordVideoFromCamera();

      expect(result, isNotNull);
      expect(result!.mediaType, IncidentMediaType.video);
      expect(result.thumbnailPath, '${tempDir.path}/mock_thumb.jpg');
      expect(result.formattedFileSize, '15.0 MB');
    });

    test('throws FileQuotaExceededException when video exceeds 50MB', () async {
      final file = createTestFile('huge.mov', 52 * 1024 * 1024); // 52 MB
      fakePicker.returnedVideo = XFile(file.path);

      expect(
        () => service.recordVideoFromCamera(),
        throwsA(isA<FileQuotaExceededException>()),
      );
    });

    test('throws UnsupportedMediaFormatException for unsupported video format', () async {
      final file = createTestFile('legacy.avi', 1024);
      fakePicker.returnedVideo = XFile(file.path);

      expect(
        () => service.recordVideoFromCamera(),
        throwsA(isA<UnsupportedMediaFormatException>()),
      );
    });
  });
}
