import '../../../../core/models/media_attachment_model.dart';
import 'feedback_category.dart';

class CreateFeedbackRequest {
  static const int maxTitleLength = 200;
  static const int maxDescriptionLength = 4000;
  static const int maxAttachmentsCount = 5;

  final String title;
  final String description;
  final FeedbackCategory category;
  final List<SelectedMediaFile> attachments;

  const CreateFeedbackRequest({
    required this.title,
    required this.description,
    this.category = FeedbackCategory.bugReport,
    this.attachments = const [],
  });

  bool get isValid =>
      title.trim().isNotEmpty &&
      title.trim().length <= maxTitleLength &&
      description.trim().isNotEmpty &&
      description.trim().length <= maxDescriptionLength &&
      attachments.length <= maxAttachmentsCount;

  String get formattedTitle {
    final cleanTitle = title.trim();
    final combined = '${category.tag} $cleanTitle';
    if (combined.length <= maxTitleLength) {
      return combined;
    }
    return cleanTitle.length <= maxTitleLength
        ? cleanTitle
        : cleanTitle.substring(0, maxTitleLength);
  }

  CreateFeedbackRequest copyWith({
    String? title,
    String? description,
    FeedbackCategory? category,
    List<SelectedMediaFile>? attachments,
  }) {
    return CreateFeedbackRequest(
      title: title ?? this.title,
      description: description ?? this.description,
      category: category ?? this.category,
      attachments: attachments ?? this.attachments,
    );
  }
}
