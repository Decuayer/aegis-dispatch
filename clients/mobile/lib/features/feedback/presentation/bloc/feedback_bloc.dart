import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/models/media_attachment_model.dart';
import '../../data/feedback_repository.dart';
import '../../data/models/create_feedback_request.dart';
import 'feedback_event.dart';
import 'feedback_state.dart';

class FeedbackBloc extends Bloc<FeedbackEvent, FeedbackState> {
  final FeedbackRepository _repository;

  FeedbackBloc({required FeedbackRepository repository})
    : _repository = repository,
      super(const FeedbackInitial()) {
    on<FeedbackTitleChanged>(_onTitleChanged);
    on<FeedbackDescriptionChanged>(_onDescriptionChanged);
    on<FeedbackCategoryChanged>(_onCategoryChanged);
    on<FeedbackMediaAdded>(_onMediaAdded);
    on<FeedbackMediaRemoved>(_onMediaRemoved);
    on<FeedbackSubmitted>(_onSubmitted);
    on<FeedbackReset>(_onReset);
  }

  void _onTitleChanged(
    FeedbackTitleChanged event,
    Emitter<FeedbackState> emit,
  ) {
    emit(
      FeedbackInitial(
        title: event.title,
        description: state.description,
        category: state.category,
        attachments: state.attachments,
      ),
    );
  }

  void _onDescriptionChanged(
    FeedbackDescriptionChanged event,
    Emitter<FeedbackState> emit,
  ) {
    emit(
      FeedbackInitial(
        title: state.title,
        description: event.description,
        category: state.category,
        attachments: state.attachments,
      ),
    );
  }

  void _onCategoryChanged(
    FeedbackCategoryChanged event,
    Emitter<FeedbackState> emit,
  ) {
    emit(
      FeedbackInitial(
        title: state.title,
        description: state.description,
        category: event.category,
        attachments: state.attachments,
      ),
    );
  }

  void _onMediaAdded(FeedbackMediaAdded event, Emitter<FeedbackState> emit) {
    final combined = List<SelectedMediaFile>.from(state.attachments)
      ..addAll(event.mediaFiles);

    if (combined.length > CreateFeedbackRequest.maxAttachmentsCount) {
      emit(
        FeedbackFailure(
          errorMessage:
              'Maximum ${CreateFeedbackRequest.maxAttachmentsCount} media attachments allowed.',
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
      return;
    }

    emit(
      FeedbackInitial(
        title: state.title,
        description: state.description,
        category: state.category,
        attachments: combined,
      ),
    );
  }

  void _onMediaRemoved(
    FeedbackMediaRemoved event,
    Emitter<FeedbackState> emit,
  ) {
    if (event.index < 0 || event.index >= state.attachments.length) return;

    final updated = List<SelectedMediaFile>.from(state.attachments)
      ..removeAt(event.index);

    emit(
      FeedbackInitial(
        title: state.title,
        description: state.description,
        category: state.category,
        attachments: updated,
      ),
    );
  }

  Future<void> _onSubmitted(
    FeedbackSubmitted event,
    Emitter<FeedbackState> emit,
  ) async {
    if (state is FeedbackSubmitting) return;

    if (state.title.trim().isEmpty) {
      emit(
        FeedbackFailure(
          errorMessage: 'Title is required.',
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
      return;
    }

    if (state.title.trim().length > CreateFeedbackRequest.maxTitleLength) {
      emit(
        FeedbackFailure(
          errorMessage:
              'Title must not exceed ${CreateFeedbackRequest.maxTitleLength} characters.',
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
      return;
    }

    if (state.description.trim().isEmpty) {
      emit(
        FeedbackFailure(
          errorMessage: 'Description is required.',
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
      return;
    }

    if (state.description.trim().length >
        CreateFeedbackRequest.maxDescriptionLength) {
      emit(
        FeedbackFailure(
          errorMessage:
              'Description must not exceed ${CreateFeedbackRequest.maxDescriptionLength} characters.',
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
      return;
    }

    if (state.attachments.length > CreateFeedbackRequest.maxAttachmentsCount) {
      emit(
        FeedbackFailure(
          errorMessage:
              'Maximum ${CreateFeedbackRequest.maxAttachmentsCount} media attachments allowed.',
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
      return;
    }

    emit(
      FeedbackSubmitting(
        title: state.title,
        description: state.description,
        category: state.category,
        attachments: state.attachments,
      ),
    );

    try {
      final request = CreateFeedbackRequest(
        title: state.title,
        description: state.description,
        category: state.category,
        attachments: state.attachments,
      );

      final response = await _repository.submitFeedback(request);

      emit(
        FeedbackSuccess(
          response: response,
          message: 'Feedback submitted successfully.',
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
    } catch (e) {
      final message = e.toString().replaceAll('Exception: ', '');
      emit(
        FeedbackFailure(
          errorMessage: message,
          title: state.title,
          description: state.description,
          category: state.category,
          attachments: state.attachments,
        ),
      );
    }
  }

  void _onReset(FeedbackReset event, Emitter<FeedbackState> emit) {
    emit(const FeedbackInitial());
  }
}
