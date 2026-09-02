import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../incident_reporting/presentation/widgets/media_preview_grid.dart';
import '../../../incident_reporting/services/media_picker_service.dart';
import '../../data/feedback_repository.dart';
import '../../data/models/create_feedback_request.dart';
import '../../data/models/feedback_category.dart';
import '../bloc/feedback_bloc.dart';
import '../bloc/feedback_event.dart';
import '../bloc/feedback_state.dart';
import '../widgets/feedback_success_dialog.dart';

class FeedbackSubmissionView extends StatelessWidget {
  final MediaPickerService? mediaPickerService;
  final FeedbackRepository? feedbackRepository;

  const FeedbackSubmissionView({
    super.key,
    this.mediaPickerService,
    this.feedbackRepository,
  });

  @override
  Widget build(BuildContext context) {
    final repo = feedbackRepository ?? RepositoryProvider.of<FeedbackRepository>(context);
    final pickerService = mediaPickerService ?? RepositoryProvider.of<MediaPickerService>(context);

    return BlocProvider(
      create: (ctx) => FeedbackBloc(repository: repo),
      child: _FeedbackSubmissionViewContent(
        mediaPickerService: pickerService,
      ),
    );
  }
}

class _FeedbackSubmissionViewContent extends StatefulWidget {
  final MediaPickerService mediaPickerService;

  const _FeedbackSubmissionViewContent({
    required this.mediaPickerService,
  });

  @override
  State<_FeedbackSubmissionViewContent> createState() =>
      _FeedbackSubmissionViewContentState();
}

class _FeedbackSubmissionViewContentState
    extends State<_FeedbackSubmissionViewContent> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  void _showErrorMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).hideCurrentSnackBar();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: AppColors.error,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  void _showMediaPickerOptions(BuildContext context, int currentAttachmentsCount) {
    if (currentAttachmentsCount >= CreateFeedbackRequest.maxAttachmentsCount) {
      _showErrorMessage(
        'Maximum ${CreateFeedbackRequest.maxAttachmentsCount} attachments allowed.',
      );
      return;
    }

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
                  leading: const Icon(Icons.photo_camera_rounded, color: AppColors.primary),
                  title: const Text('Take Photo'),
                  subtitle: const Text('Capture photo with camera (max 10 MB)'),
                  onTap: () async {
                    Navigator.pop(sheetContext);
                    try {
                      final photo = await widget.mediaPickerService.pickImageFromCamera();
                      if (photo != null && context.mounted) {
                        context.read<FeedbackBloc>().add(FeedbackMediaAdded([photo]));
                      }
                    } catch (e) {
                      _showErrorMessage(e.toString().replaceAll('Exception: ', ''));
                    }
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.videocam_rounded, color: AppColors.primary),
                  title: const Text('Record Video'),
                  subtitle: const Text('Record video with camera (max 50 MB)'),
                  onTap: () async {
                    Navigator.pop(sheetContext);
                    try {
                      final video = await widget.mediaPickerService.recordVideoFromCamera();
                      if (video != null && context.mounted) {
                        context.read<FeedbackBloc>().add(FeedbackMediaAdded([video]));
                      }
                    } catch (e) {
                      _showErrorMessage(e.toString().replaceAll('Exception: ', ''));
                    }
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.perm_media_rounded, color: AppColors.primary),
                  title: const Text('Choose from Gallery'),
                  subtitle: const Text('Select photos or videos (batch)'),
                  onTap: () async {
                    Navigator.pop(sheetContext);
                    try {
                      final media = await widget.mediaPickerService.pickMultipleMedia();
                      if (media.isNotEmpty && context.mounted) {
                        context.read<FeedbackBloc>().add(FeedbackMediaAdded(media));
                      }
                    } catch (e) {
                      _showErrorMessage(e.toString().replaceAll('Exception: ', ''));
                    }
                  },
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _onSubmit() {
    if (_formKey.currentState?.validate() ?? false) {
      context.read<FeedbackBloc>().add(const FeedbackSubmitted());
    }
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<FeedbackBloc, FeedbackState>(
      listener: (context, state) {
        if (state is FeedbackSuccess) {
          showDialog(
            context: context,
            barrierDismissible: false,
            builder: (dialogCtx) => FeedbackSuccessDialog(
              response: state.response,
              onDismiss: () {
                Navigator.of(dialogCtx).pop();
                Navigator.of(context).pop();
              },
            ),
          );
        } else if (state is FeedbackFailure) {
          _showErrorMessage(state.errorMessage);
        }
      },
      builder: (context, state) {
        final isSubmitting = state is FeedbackSubmitting;

        return Scaffold(
          backgroundColor: AppColors.background,
          appBar: AppBar(
            title: const Text('Send Feedback'),
          ),
          body: Stack(
            children: [
              SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // Category Selector Card
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Feedback Category',
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 12),
                            Wrap(
                              spacing: 8,
                              runSpacing: 8,
                              children: FeedbackCategory.values.map((cat) {
                                final isSelected = state.category == cat;
                                return ChoiceChip(
                                  avatar: Icon(
                                    cat.icon,
                                    size: 18,
                                    color: isSelected ? Colors.white : AppColors.textSecondary,
                                  ),
                                  label: Text(cat.label),
                                  selected: isSelected,
                                  selectedColor: AppColors.primary,
                                  backgroundColor: AppColors.background,
                                  labelStyle: TextStyle(
                                    color: isSelected ? Colors.white : AppColors.textPrimary,
                                    fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal,
                                    fontSize: 13,
                                  ),
                                  onSelected: isSubmitting
                                      ? null
                                      : (_) {
                                          context
                                              .read<FeedbackBloc>()
                                              .add(FeedbackCategoryChanged(cat));
                                        },
                                );
                              }).toList(),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // Form Fields Card
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Feedback Details',
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _titleController,
                              enabled: !isSubmitting,
                              maxLength: CreateFeedbackRequest.maxTitleLength,
                              decoration: const InputDecoration(
                                labelText: 'Title *',
                                hintText: 'Brief summary of issue or suggestion',
                                prefixIcon: Icon(Icons.title_rounded, size: 20),
                              ),
                              onChanged: (val) {
                                context.read<FeedbackBloc>().add(FeedbackTitleChanged(val));
                              },
                              validator: (val) {
                                if (val == null || val.trim().isEmpty) {
                                  return 'Title is required.';
                                }
                                if (val.trim().length > CreateFeedbackRequest.maxTitleLength) {
                                  return 'Title must not exceed ${CreateFeedbackRequest.maxTitleLength} characters.';
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _descriptionController,
                              enabled: !isSubmitting,
                              minLines: 4,
                              maxLines: 8,
                              maxLength: CreateFeedbackRequest.maxDescriptionLength,
                              decoration: const InputDecoration(
                                labelText: 'Description *',
                                hintText: 'Describe details, steps to reproduce, or operational impact...',
                                alignLabelWithHint: true,
                              ),
                              onChanged: (val) {
                                context.read<FeedbackBloc>().add(FeedbackDescriptionChanged(val));
                              },
                              validator: (val) {
                                if (val == null || val.trim().isEmpty) {
                                  return 'Description is required.';
                                }
                                if (val.trim().length > CreateFeedbackRequest.maxDescriptionLength) {
                                  return 'Description must not exceed ${CreateFeedbackRequest.maxDescriptionLength} characters.';
                                }
                                return null;
                              },
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // Media Attachments Card
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                const Text(
                                  'Media Attachments',
                                  style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w700,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                                Text(
                                  '${state.attachments.length}/${CreateFeedbackRequest.maxAttachmentsCount}',
                                  style: const TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            const Text(
                              'Attach up to 5 photos (max 10MB) or videos (max 50MB).',
                              style: TextStyle(
                                fontSize: 12,
                                color: AppColors.textMuted,
                              ),
                            ),
                            const SizedBox(height: 12),
                            if (state.attachments.isNotEmpty) ...[
                              MediaPreviewGrid(
                                mediaFiles: state.attachments,
                                onRemove: isSubmitting
                                    ? (_) {}
                                    : (index) {
                                        context
                                            .read<FeedbackBloc>()
                                            .add(FeedbackMediaRemoved(index));
                                      },
                              ),
                              const SizedBox(height: 12),
                            ],
                            OutlinedButton.icon(
                              style: OutlinedButton.styleFrom(
                                minimumSize: const Size.fromHeight(46),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                side: const BorderSide(color: AppColors.primary),
                              ),
                              icon: const Icon(Icons.add_photo_alternate_outlined, size: 20),
                              label: Text(
                                state.attachments.isEmpty ? 'Attach Media' : 'Add More Media',
                                style: const TextStyle(fontWeight: FontWeight.w600),
                              ),
                              onPressed: isSubmitting
                                  ? null
                                  : () => _showMediaPickerOptions(context, state.attachments.length),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),

                      // Submit Button
                      ElevatedButton(
                        onPressed: isSubmitting ? null : _onSubmit,
                        child: isSubmitting
                            ? const SizedBox(
                                height: 22,
                                width: 22,
                                child: CircularProgressIndicator(
                                  color: Colors.white,
                                  strokeWidth: 2.2,
                                ),
                              )
                            : const Text('Submit Feedback'),
                      ),
                      const SizedBox(height: 24),
                    ],
                  ),
                ),
              ),

              // Full Screen In-Flight Overlay
              if (isSubmitting)
                Container(
                  color: Colors.black26,
                  child: const Center(
                    child: Card(
                      elevation: 4,
                      child: Padding(
                        padding: EdgeInsets.symmetric(horizontal: 24, vertical: 20),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            CircularProgressIndicator(strokeWidth: 2.5),
                            SizedBox(width: 16),
                            Text(
                              'Sending feedback...',
                              style: TextStyle(
                                fontWeight: FontWeight.w600,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}
