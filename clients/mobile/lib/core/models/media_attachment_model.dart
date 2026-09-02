import 'dart:io';
import '../../features/incident_reporting/data/models/create_incident_request_model.dart';

class FileQuotaExceededException implements Exception {
  final String message;
  const FileQuotaExceededException(this.message);

  @override
  String toString() => message;
}

class UnsupportedMediaFormatException implements Exception {
  final String message;
  const UnsupportedMediaFormatException(this.message);

  @override
  String toString() => message;
}

class MediaValidationRules {
  static const int maxPhotoSizeBytes = 10 * 1024 * 1024; // 10 MB
  static const int maxVideoSizeBytes = 50 * 1024 * 1024; // 50 MB

  static const Set<String> allowedImageExtensions = {
    'jpg',
    'jpeg',
    'png',
    'webp',
  };

  static const Set<String> allowedVideoExtensions = {
    'mp4',
    'mov',
  };
}

class MediaAttachmentModel {
  final File file;
  final IncidentMediaType mediaType;
  final int fileSizeBytes;
  final String fileName;
  final String? thumbnailPath;
  final int? durationSeconds;

  const MediaAttachmentModel({
    required this.file,
    required this.mediaType,
    required this.fileSizeBytes,
    required this.fileName,
    this.thumbnailPath,
    this.durationSeconds,
  });

  bool get isVideo => mediaType == IncidentMediaType.video;
  bool get isPhoto => mediaType == IncidentMediaType.photo;

  String get formattedFileSize {
    if (fileSizeBytes < 1024) {
      return '$fileSizeBytes B';
    } else if (fileSizeBytes < 1024 * 1024) {
      final kb = fileSizeBytes / 1024;
      return '${kb.toStringAsFixed(1)} KB';
    } else {
      final mb = fileSizeBytes / (1024 * 1024);
      return '${mb.toStringAsFixed(1)} MB';
    }
  }

  MediaAttachmentModel copyWith({
    File? file,
    IncidentMediaType? mediaType,
    int? fileSizeBytes,
    String? fileName,
    String? thumbnailPath,
    int? durationSeconds,
  }) {
    return MediaAttachmentModel(
      file: file ?? this.file,
      mediaType: mediaType ?? this.mediaType,
      fileSizeBytes: fileSizeBytes ?? this.fileSizeBytes,
      fileName: fileName ?? this.fileName,
      thumbnailPath: thumbnailPath ?? this.thumbnailPath,
      durationSeconds: durationSeconds ?? this.durationSeconds,
    );
  }
}

// Backward-compatible alias for existing implementations
typedef SelectedMediaFile = MediaAttachmentModel;
