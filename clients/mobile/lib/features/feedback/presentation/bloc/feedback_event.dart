import '../../../../core/models/media_attachment_model.dart';
import '../../data/models/feedback_category.dart';

abstract class FeedbackEvent {
  const FeedbackEvent();
}

class FeedbackTitleChanged extends FeedbackEvent {
  final String title;
  const FeedbackTitleChanged(this.title);
}

class FeedbackDescriptionChanged extends FeedbackEvent {
  final String description;
  const FeedbackDescriptionChanged(this.description);
}

class FeedbackCategoryChanged extends FeedbackEvent {
  final FeedbackCategory category;
  const FeedbackCategoryChanged(this.category);
}

class FeedbackMediaAdded extends FeedbackEvent {
  final List<SelectedMediaFile> mediaFiles;
  const FeedbackMediaAdded(this.mediaFiles);
}

class FeedbackMediaRemoved extends FeedbackEvent {
  final int index;
  const FeedbackMediaRemoved(this.index);
}

class FeedbackSubmitted extends FeedbackEvent {
  const FeedbackSubmitted();
}

class FeedbackReset extends FeedbackEvent {
  const FeedbackReset();
}
