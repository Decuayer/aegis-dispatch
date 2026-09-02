import 'dart:io';
import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/models/media_attachment_model.dart';

class MediaPreviewGrid extends StatelessWidget {
  final List<SelectedMediaFile> mediaFiles;
  final void Function(int index) onRemove;

  const MediaPreviewGrid({
    super.key,
    required this.mediaFiles,
    required this.onRemove,
  });

  void _showMediaPreview(BuildContext context, SelectedMediaFile media) {
    showDialog(
      context: context,
      barrierDismissible: true,
      builder: (dialogContext) {
        return Dialog(
          backgroundColor: Colors.transparent,
          insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
          child: media.isVideo
              ? _VideoPreviewCard(media: media)
              : _PhotoPreviewCard(media: media),
        );
      },
    );
  }

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
        final isVideo = media.isVideo;
        final hasThumbnail = media.thumbnailPath != null &&
            File(media.thumbnailPath!).existsSync();

        return Stack(
          children: [
            Positioned.fill(
              child: GestureDetector(
                onTap: () => _showMediaPreview(context, media),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(10),
                  child: isVideo
                      ? (hasThumbnail
                          ? Stack(
                              fit: StackFit.expand,
                              children: [
                                Image.file(
                                  File(media.thumbnailPath!),
                                  fit: BoxFit.cover,
                                ),
                                Container(
                                  color: Colors.black26,
                                  child: const Center(
                                    child: Icon(
                                      Icons.play_circle_fill_rounded,
                                      color: Colors.white,
                                      size: 32,
                                    ),
                                  ),
                                ),
                              ],
                            )
                          : Container(
                              color: AppColors.textPrimary,
                              child: const Center(
                                child: Icon(
                                  Icons.videocam_rounded,
                                  color: Colors.white,
                                  size: 36,
                                ),
                              ),
                            ))
                      : Image.file(
                          media.file,
                          fit: BoxFit.cover,
                        ),
                ),
              ),
            ),
            // Video badge with play icon & duration
            if (isVideo)
              Positioned(
                bottom: 4,
                left: 4,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.75),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.play_arrow_rounded, size: 12, color: Colors.white),
                      const SizedBox(width: 2),
                      Text(
                        media.durationSeconds != null
                            ? '${media.durationSeconds! ~/ 60}:${(media.durationSeconds! % 60).toString().padLeft(2, '0')}'
                            : 'Video',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            // Human-readable file size badge
            Positioned(
              bottom: 4,
              right: 4,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                decoration: BoxDecoration(
                  color: Colors.black.withValues(alpha: 0.75),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  media.formattedFileSize,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 9,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            ),
            // Remove button
            Positioned(
              top: 4,
              right: 4,
              child: GestureDetector(
                behavior: HitTestBehavior.opaque,
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

class _PhotoPreviewCard extends StatelessWidget {
  final SelectedMediaFile media;

  const _PhotoPreviewCard({required this.media});

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(maxHeight: 520),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    media.fileName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ),
                Text(
                  media.formattedFileSize,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.pop(context),
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
          ),
          Flexible(
            child: ClipRRect(
              borderRadius: const BorderRadius.vertical(bottom: Radius.circular(16)),
              child: InteractiveViewer(
                minScale: 0.8,
                maxScale: 3.5,
                child: Image.file(
                  media.file,
                  fit: BoxFit.contain,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _VideoPreviewCard extends StatelessWidget {
  final SelectedMediaFile media;

  const _VideoPreviewCard({required this.media});

  @override
  Widget build(BuildContext context) {
    final hasThumbnail = media.thumbnailPath != null &&
        File(media.thumbnailPath!).existsSync();

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const Icon(Icons.videocam_rounded, color: AppColors.primary),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  media.fileName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                ),
              ),
              IconButton(
                icon: const Icon(Icons.close),
                onPressed: () => Navigator.pop(context),
                visualDensity: VisualDensity.compact,
              ),
            ],
          ),
          const SizedBox(height: 16),
          ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: Container(
              height: 200,
              color: AppColors.textPrimary,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  if (hasThumbnail)
                    Image.file(
                      File(media.thumbnailPath!),
                      fit: BoxFit.cover,
                    ),
                  Container(
                    color: Colors.black38,
                    child: const Center(
                      child: Icon(
                        Icons.play_circle_fill_rounded,
                        color: Colors.white,
                        size: 54,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.background,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildInfoColumn('Format', media.fileName.split('.').last.toUpperCase()),
                _buildInfoColumn('Size', media.formattedFileSize),
                _buildInfoColumn(
                  'Duration',
                  media.durationSeconds != null
                      ? '${media.durationSeconds}s'
                      : 'Clip',
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoColumn(String title, String value) {
    return Column(
      children: [
        Text(
          title,
          style: const TextStyle(fontSize: 11, color: AppColors.textMuted),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: AppColors.textPrimary,
          ),
        ),
      ],
    );
  }
}
