import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';
import '../../data/models/create_incident_request_model.dart';
import '../../services/media_picker_service.dart';

class MediaPreviewGrid extends StatelessWidget {
  final List<SelectedMediaFile> mediaFiles;
  final void Function(int index) onRemove;

  const MediaPreviewGrid({
    super.key,
    required this.mediaFiles,
    required this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    if (mediaFiles.isEmpty) {
      return const SizedBox.shrink();
    }

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        crossAxisSpacing: 10,
        mainAxisSpacing: 10,
        childAspectRatio: 1,
      ),
      itemCount: mediaFiles.length,
      itemBuilder: (context, index) {
        final media = mediaFiles[index];
        final isVideo = media.mediaType == IncidentMediaType.video;

        return Stack(
          children: [
            Positioned.fill(
              child: ClipRRect(
                borderRadius: BorderRadius.circular(10),
                child: isVideo
                    ? Container(
                        color: AppColors.textPrimary,
                        child: const Center(
                          child: Icon(
                            Icons.videocam_rounded,
                            color: Colors.white,
                            size: 36,
                          ),
                        ),
                      )
                    : Image.file(
                        media.file,
                        fit: BoxFit.cover,
                      ),
              ),
            ),
            if (isVideo)
              Positioned(
                bottom: 4,
                left: 4,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.7),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: const Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.play_arrow, size: 12, color: Colors.white),
                      SizedBox(width: 2),
                      Text(
                        'Video',
                        style: TextStyle(color: Colors.white, fontSize: 10),
                      ),
                    ],
                  ),
                ),
              ),
            Positioned(
              top: 4,
              right: 4,
              child: GestureDetector(
                onTap: () => onRemove(index),
                child: Container(
                  padding: const EdgeInsets.all(4),
                  decoration: const BoxDecoration(
                    color: Colors.black54,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.close,
                    size: 14,
                    color: Colors.white,
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}
