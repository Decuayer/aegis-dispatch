import 'dart:io';
import 'package:image_picker/image_picker.dart';
import '../../../../core/models/media_attachment_model.dart';
import '../data/models/create_incident_request_model.dart';
import 'thumbnail_generator_service.dart';

export '../../../../core/models/media_attachment_model.dart';

class MediaPickerService {
  final ImagePicker _picker;
  final ThumbnailGeneratorService _thumbnailService;

  // Backward-compatible constant (50 MB)
  static const int maxFileSizeBytes = MediaValidationRules.maxVideoSizeBytes;

  MediaPickerService({
    ImagePicker? picker,
    ThumbnailGeneratorService? thumbnailService,
  }) : _picker = picker ?? ImagePicker(),
       _thumbnailService = thumbnailService ?? ThumbnailGeneratorService();

  /// Captures a single photo from the camera.
  Future<SelectedMediaFile?> pickImageFromCamera() async {
    final xFile = await _picker.pickImage(
      source: ImageSource.camera,
      imageQuality: 80,
    );

    if (xFile == null) return null;
    return _validateAndProcessFile(File(xFile.path), IncidentMediaType.photo);
  }

  /// Picks multiple photos from the gallery.
  Future<List<SelectedMediaFile>> pickImagesFromGallery() async {
    final xFiles = await _picker.pickMultiImage(imageQuality: 80);
    if (xFiles.isEmpty) return const [];

    final List<SelectedMediaFile> results = [];
    for (final xFile in xFiles) {
      final processed = await _validateAndProcessFile(
        File(xFile.path),
        IncidentMediaType.photo,
      );
      if (processed != null) {
        results.add(processed);
      }
    }
    return results;
  }

  /// Records a video from the native camera.
  Future<SelectedMediaFile?> recordVideoFromCamera({
    Duration maxDuration = const Duration(seconds: 30),
  }) async {
    final xFile = await _picker.pickVideo(
      source: ImageSource.camera,
      maxDuration: maxDuration,
    );

    if (xFile == null) return null;
    return _validateAndProcessFile(File(xFile.path), IncidentMediaType.video);
  }

  /// Picks a single video from the gallery.
  Future<SelectedMediaFile?> pickVideoFromGallery() async {
    final xFile = await _picker.pickVideo(source: ImageSource.gallery);
    if (xFile == null) return null;
    return _validateAndProcessFile(File(xFile.path), IncidentMediaType.video);
  }

  /// Picks multiple mixed media (photos and videos) from the gallery.
  Future<List<SelectedMediaFile>> pickMultipleMedia() async {
    final xFiles = await _picker.pickMultipleMedia(imageQuality: 80);
    if (xFiles.isEmpty) return const [];

    final List<SelectedMediaFile> results = [];
    for (final xFile in xFiles) {
      final ext = xFile.path.split('.').last.toLowerCase();
      final isVideo = MediaValidationRules.allowedVideoExtensions.contains(ext);
      final mediaType =
          isVideo ? IncidentMediaType.video : IncidentMediaType.photo;

      final processed = await _validateAndProcessFile(
        File(xFile.path),
        mediaType,
      );
      if (processed != null) {
        results.add(processed);
      }
    }
    return results;
  }

  /// Validates file existence, format extension, size quota, and generates thumbnails for videos.
  Future<SelectedMediaFile?> _validateAndProcessFile(
    File file,
    IncidentMediaType mediaType,
  ) async {
    if (!file.existsSync()) return null;

    final name = file.path.split(Platform.pathSeparator).last;
    final ext = name.contains('.') ? name.split('.').last.toLowerCase() : '';
    final sizeBytes = file.lengthSync();

    // 1. Format Validation
    if (mediaType == IncidentMediaType.photo) {
      if (!MediaValidationRules.allowedImageExtensions.contains(ext)) {
        throw UnsupportedMediaFormatException(
          'Unsupported photo format: .$ext. Allowed: jpg, jpeg, png, webp.',
        );
      }
      if (sizeBytes > MediaValidationRules.maxPhotoSizeBytes) {
        throw const FileQuotaExceededException(
          'Photo file size exceeds the 10 MB limit.',
        );
      }
    } else {
      if (!MediaValidationRules.allowedVideoExtensions.contains(ext)) {
        throw UnsupportedMediaFormatException(
          'Unsupported video format: .$ext. Allowed: mp4, mov.',
        );
      }
      if (sizeBytes > MediaValidationRules.maxVideoSizeBytes) {
        throw const FileQuotaExceededException(
          'Video file size exceeds the 50 MB limit.',
        );
      }
    }

    // 2. Thumbnail Generation for Videos
    String? thumbnailPath;
    if (mediaType == IncidentMediaType.video) {
      thumbnailPath = await _thumbnailService.generateVideoThumbnail(file);
    }

    return SelectedMediaFile(
      file: file,
      mediaType: mediaType,
      fileSizeBytes: sizeBytes,
      fileName: name,
      thumbnailPath: thumbnailPath,
    );
  }
}
