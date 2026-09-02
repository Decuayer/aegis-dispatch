import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../../../core/constants/app_colors.dart';
import '../../bloc/incident_report_bloc.dart';
import '../../bloc/incident_report_event.dart';
import '../../bloc/incident_report_state.dart';
import '../../../services/media_picker_service.dart';
import '../../widgets/media_preview_grid.dart';

class StepMediaCapture extends StatelessWidget {
  final MediaPickerService mediaPickerService;

  const StepMediaCapture({
    super.key,
    required this.mediaPickerService,
  });

  void _showErrorMessage(BuildContext context, Object error) {
    if (!context.mounted) return;
    final message = error.toString().replaceAll('Exception: ', '');
    ScaffoldMessenger.of(context).hideCurrentSnackBar();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: AppColors.error,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

    Future<void> _handleTakePhoto(BuildContext context) async {
    try {
      final photo = await mediaPickerService.pickImageFromCamera();
      if (!context.mounted) return;
      if (photo != null) {
        context.read<IncidentReportBloc>().add(MediaFilesAdded([photo]));
      }
    } catch (e) {
      if (!context.mounted) return;
      _showErrorMessage(context, e);
    }
  }

  Future<void> _handleRecordVideo(BuildContext context) async {
    try {
      final video = await mediaPickerService.recordVideoFromCamera();
      if (!context.mounted) return;
      if (video != null) {
        context.read<IncidentReportBloc>().add(MediaFilesAdded([video]));
      }
    } catch (e) {
      if (!context.mounted) return;
      _showErrorMessage(context, e);
    }
  }

  Future<void> _handlePickMultipleMedia(BuildContext context) async {
    try {
      final media = await mediaPickerService.pickMultipleMedia();
      if (!context.mounted) return;
      if (media.isNotEmpty) {
        context.read<IncidentReportBloc>().add(MediaFilesAdded(media));
      }
    } catch (e) {
      if (!context.mounted) return;
      _showErrorMessage(context, e);
    }
  }

  Future<void> _handlePickVideoFromGallery(BuildContext context) async {
    try {
      final video = await mediaPickerService.pickVideoFromGallery();
      if (!context.mounted) return;
      if (video != null) {
        context.read<IncidentReportBloc>().add(MediaFilesAdded([video]));
      }
    } catch (e) {
      if (!context.mounted) return;
      _showErrorMessage(context, e);
    }
  }

  Future<void> _handlePickPhotosFromGallery(BuildContext context) async {
    try {
      final photos = await mediaPickerService.pickImagesFromGallery();
      if (!context.mounted) return;
      if (photos.isNotEmpty) {
        context.read<IncidentReportBloc>().add(MediaFilesAdded(photos));
      }
    } catch (e) {
      if (!context.mounted) return;
      _showErrorMessage(context, e);
    }
  }


  void _showGalleryPickerOptions(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (sheetContext) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                ListTile(
                  leading: const Icon(Icons.perm_media_rounded, color: AppColors.primary),
                  title: const Text('Photos & Videos (Batch)'),
                  subtitle: const Text('Select multiple photos and videos together'),
                  onTap: () {
                    Navigator.pop(sheetContext);
                    _handlePickMultipleMedia(context);
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.photo_library_rounded, color: AppColors.primary),
                  title: const Text('Photos Only'),
                  subtitle: const Text('Select photos from gallery'),
                  onTap: () {
                    Navigator.pop(sheetContext);
                    _handlePickPhotosFromGallery(context);
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.video_library_rounded, color: AppColors.primary),
                  title: const Text('Video Only'),
                  subtitle: const Text('Select a video file from gallery'),
                  onTap: () {
                    Navigator.pop(sheetContext);
                    _handlePickVideoFromGallery(context);
                  },
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<IncidentReportBloc, IncidentReportState>(
      builder: (context, state) {
        return SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'Capture Visual Evidence',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                'Attach photos or short video clips to help the response team prepare adequate resources.',
                style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 20),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _handleTakePhoto(context),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      icon: const Icon(Icons.camera_alt_rounded),
                      label: const Text('Photo'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _handleRecordVideo(context),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      icon: const Icon(Icons.videocam_rounded),
                      label: const Text('Video'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _showGalleryPickerOptions(context),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      icon: const Icon(Icons.photo_library_rounded),
                      label: const Text('Gallery'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),
              if (state.selectedMediaFiles.isEmpty)
                Container(
                  padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 20),
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: AppColors.border,
                      style: BorderStyle.solid,
                    ),
                  ),
                  child: const Column(
                    children: [
                      Icon(
                        Icons.add_photo_alternate_outlined,
                        size: 48,
                        color: AppColors.textMuted,
                      ),
                      SizedBox(height: 12),
                      Text(
                        'No visual media attached yet',
                        style: TextStyle(
                          fontWeight: FontWeight.w600,
                          color: AppColors.textSecondary,
                        ),
                      ),
                      SizedBox(height: 4),
                      Text(
                        'Media is optional but highly recommended.',
                        style: TextStyle(fontSize: 12, color: AppColors.textMuted),
                      ),
                    ],
                  ),
                )
              else
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Attached Media (${state.selectedMediaFiles.length})',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 14,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 12),
                    MediaPreviewGrid(
                      mediaFiles: state.selectedMediaFiles,
                      onRemove: (index) {
                        context
                            .read<IncidentReportBloc>()
                            .add(MediaFileRemoved(index));
                      },
                    ),  
                  ],
                ),
            ],
          ),
        );
      },
    );
  }
}
