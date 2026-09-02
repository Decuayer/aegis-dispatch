import '../../../../core/models/media_attachment_model.dart';
import '../../data/models/create_feedback_request.dart';
import '../../data/models/feedback_category.dart';
import '../../data/models/feedback_response_model.dart';

abstract class FeedbackState {
  final String title;
  final String description;
  final FeedbackCategory category;
  final List<SelectedMediaFile> attachments;

  const FeedbackState({
    this.title = '',
    this.description = '',
    this.category = FeedbackCategory.bugReport,
    this.attachments = const [],
  });

  bool get isTitleValid =>
      title.trim().isNotEmpty &&
      title.trim().length <= CreateFeedbackRequest.maxTitleLength;

  bool get isDescriptionValid =>
      description.trim().isNotEmpty &&
      description.trim().length <= CreateFeedbackRequest.maxDescriptionLength;

  bool get areAttachmentsValid =>
      attachments.length <= CreateFeedbackRequest.maxAttachmentsCount;

  bool get canSubmit =>
      isTitleValid && isDescriptionValid && areAttachmentsValid;
}

class FeedbackInitial extends FeedbackState {
  const FeedbackInitial({
    super.title,
    super.description,
    super.category,
    super.attachments,
  });
}

class FeedbackSubmitting extends FeedbackState {
  const FeedbackSubmitting({
    super.title,
    super.description,
    super.category,
    super.attachments,
  });
}

class FeedbackSuccess extends FeedbackState {
  final FeedbackResponseModel response;
  final String message;

  const FeedbackSuccess({
    required this.response,
    this.message = 'Feedback submitted successfully.',
    super.title,
    super.description,
    super.category,
    super.attachments,
  });
}

class FeedbackFailure extends FeedbackState {
  final String errorMessage;

  const FeedbackFailure({
    required this.errorMessage,
    super.title,
    super.description,
    super.category,
    super.attachments,
  });
}
