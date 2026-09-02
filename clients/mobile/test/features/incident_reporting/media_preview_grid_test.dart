import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/models/media_attachment_model.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/create_incident_request_model.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/presentation/widgets/media_preview_grid.dart';

void main() {
  late Directory tempDir;
  late File samplePhoto;
  late File sampleVideo;

  setUp(() async {
    tempDir = await Directory.systemTemp.createTemp('grid_test_');
    samplePhoto = File('${tempDir.path}/sample.jpg')
      ..writeAsBytesSync(List.filled(1024 * 500, 0)); // 500 KB
    sampleVideo = File('${tempDir.path}/sample.mp4')
      ..writeAsBytesSync(List.filled(1024 * 1024 * 3, 0)); // 3 MB
  });

  tearDown(() async {
    if (await tempDir.exists()) {
      await tempDir.delete(recursive: true);
    }
  });

  Widget createWidget(List<SelectedMediaFile> files, void Function(int) onRemove) {
    return MaterialApp(
      home: Scaffold(
        body: MediaPreviewGrid(
          mediaFiles: files,
          onRemove: onRemove,
        ),
      ),
    );
  }

  group('MediaPreviewGrid Widget Tests', () {
    testWidgets('renders empty SizedBox when mediaFiles is empty', (tester) async {
      await tester.pumpWidget(createWidget([], (_) {}));
      expect(find.byType(GridView), findsNothing);
    });

    testWidgets('renders media cards, size badge, and triggers onRemove', (tester) async {
      int? removedIndex;

      final files = [
        SelectedMediaFile(
          file: samplePhoto,
          mediaType: IncidentMediaType.photo,
          fileSizeBytes: 500 * 1024,
          fileName: 'sample.jpg',
        ),
        SelectedMediaFile(
          file: sampleVideo,
          mediaType: IncidentMediaType.video,
          fileSizeBytes: 3 * 1024 * 1024,
          fileName: 'sample.mp4',
          durationSeconds: 15,
        ),
      ];

      await tester.pumpWidget(createWidget(files, (index) {
        removedIndex = index;
      }));

      // Verify human-readable size badges
      expect(find.text('500.0 KB'), findsOneWidget);
      expect(find.text('3.0 MB'), findsOneWidget);

      // Verify video badge
      expect(find.text('0:15'), findsOneWidget);

      // Tap remove button on second item
      final removeButtons = find.byIcon(Icons.close);
      expect(removeButtons, findsNWidgets(2));

      await tester.tap(removeButtons.last);
      await tester.pumpAndSettle();

      expect(removedIndex, 1);
    });
  });
}
