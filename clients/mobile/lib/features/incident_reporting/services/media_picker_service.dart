import 'dart:io';
import 'package:image_picker/image_picker.dart';
import '../data/models/create_incident_request_model.dart';

class SelectedMediaFile {
  final File file;
  final IncidentMediaType mediaType;
  final int fileSizeBytes;
  final String fileName;

  const SelectedMediaFile({
    required this.file,
    required this.mediaType,
    required this.fileSizeBytes,
    required this.fileName,
  });
}

class MediaPickerService {
  final ImagePicker _picker;
  static const int maxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

  MediaPickerService({ImagePicker? picker}) : _picker = picker ?? ImagePicker();

  Future<SelectedMediaFile?> pickImageFromCamera() async {
    final xFile = await _picker.pickImage(
      source: ImageSource.camera,
      imageQuality: 80,
    );

    if (xFile == null) return null;
    return _processFile(File(xFile.path), IncidentMediaType.photo);
  }

  Future<List<SelectedMediaFile>> pickImagesFromGallery() async {
    final xFiles = await _picker.pickMultiImage(
      imageQuality: 80,
    );

    if (xFiles.isEmpty) return const [];

    final List<SelectedMediaFile> results = [];
    for (final xFile in xFiles) {
      final processed = _processFile(File(xFile.path), IncidentMediaType.photo);
      if (processed != null) {
        results.add(processed);
      }
    }
    return results;
  }

  Future<SelectedMediaFile?> recordVideoFromCamera({
    Duration maxDuration = const Duration(seconds: 30),
  }) async {
    final xFile = await _picker.pickVideo(
      source: ImageSource.camera,
      maxDuration: maxDuration,
    );

    if (xFile == null) return null;
    return _processFile(File(xFile.path), IncidentMediaType.video);
  }

  SelectedMediaFile? _processFile(File file, IncidentMediaType mediaType) {
    if (!file.existsSync()) return null;

    final sizeBytes = file.lengthSync();
    if (sizeBytes > maxFileSizeBytes) {
      throw Exception('File size exceeds the maximum limit of 50MB.');
    }

    final name = file.path.split(Platform.pathSeparator).last;
    return SelectedMediaFile(
      file: file,
      mediaType: mediaType,
      fileSizeBytes: sizeBytes,
      fileName: name,
    );
  }
}
