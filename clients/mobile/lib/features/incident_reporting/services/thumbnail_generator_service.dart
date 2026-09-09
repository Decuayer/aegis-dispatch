import 'dart:io';
import 'package:flutter_video_thumbnail_plus/flutter_video_thumbnail_plus.dart';
import 'package:path_provider/path_provider.dart';

typedef VideoThumbnailGenerator =
    Future<String?> Function({
      required String video,
      String? thumbnailPath,
      ImageFormat imageFormat,
      int maxHeight,
      int quality,
    });

typedef TempDirectoryProvider = Future<Directory> Function();

class ThumbnailGeneratorService {
  final VideoThumbnailGenerator _thumbnailGenerator;
  final TempDirectoryProvider _tempDirectoryProvider;

  ThumbnailGeneratorService({
    VideoThumbnailGenerator? thumbnailGenerator,
    TempDirectoryProvider? tempDirectoryProvider,
  }) : _thumbnailGenerator =
           thumbnailGenerator ?? FlutterVideoThumbnailPlus.thumbnailFile,
       _tempDirectoryProvider = tempDirectoryProvider ?? getTemporaryDirectory;

  /// Generates a lightweight JPEG thumbnail for the provided video file.
  Future<String?> generateVideoThumbnail(File videoFile) async {
    if (!videoFile.existsSync()) return null;

    try {
      final tempDir = await _tempDirectoryProvider();
      final thumbnailPath = await _thumbnailGenerator(
        video: videoFile.path,
        thumbnailPath: tempDir.path,
        imageFormat: ImageFormat.jpeg,
        maxHeight: 256,
        quality: 75,
      );

      return thumbnailPath;
    } catch (_) {
      // Return null when generation fails to allow fallback placeholder
      return null;
    }
  }

  /// Deletes the cached thumbnail file from storage to avoid disk bloat.
  Future<void> deleteThumbnail(String? thumbnailPath) async {
    if (thumbnailPath == null || thumbnailPath.isEmpty) return;

    try {
      final file = File(thumbnailPath);
      if (await file.exists()) {
        await file.delete();
      }
    } catch (_) {
      // Silently ignore cleanup failures
    }
  }
}
