import 'package:flutter_bloc/flutter_bloc.dart';
import '../../data/models/create_incident_request_model.dart';
import '../../data/repositories/incident_repository.dart';
import '../../services/location_service.dart';
import 'incident_report_event.dart';
import 'incident_report_state.dart';

class IncidentReportBloc extends Bloc<IncidentReportEvent, IncidentReportState> {
  final IncidentRepository _incidentRepository;
  final LocationService _locationService;

  IncidentReportBloc({
    required IncidentRepository incidentRepository,
    required LocationService locationService,
  })  : _incidentRepository = incidentRepository,
        _locationService = locationService,
        super(const IncidentReportState()) {
    on<LoadEmergencyCodesStarted>(_onLoadEmergencyCodesStarted);
    on<StepChanged>(_onStepChanged);
    on<CategorySelected>(_onCategorySelected);
    on<EmergencyCodeSelected>(_onEmergencyCodeSelected);
    on<MediaFilesAdded>(_onMediaFilesAdded);
    on<MediaFileRemoved>(_onMediaFileRemoved);
    on<LocationRequested>(_onLocationRequested);
    on<DescriptionChanged>(_onDescriptionChanged);
    on<SubmitIncidentReportRequested>(_onSubmitIncidentReportRequested);
    on<ResetWizardState>(_onResetWizardState);
  }

  Future<void> _onLoadEmergencyCodesStarted(
    LoadEmergencyCodesStarted event,
    Emitter<IncidentReportState> emit,
  ) async {
    emit(state.copyWith(isLoadingCodes: true, clearErrorMessage: true));
    try {
      final codes = await _incidentRepository.getEmergencyCodes();
      emit(state.copyWith(
        emergencyCodes: codes,
        isLoadingCodes: false,
      ));
    } catch (e) {
      emit(state.copyWith(
        isLoadingCodes: false,
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      ));
    }
  }

  void _onStepChanged(
    StepChanged event,
    Emitter<IncidentReportState> emit,
  ) {
    if (event.newStep >= 0 && event.newStep <= 2) {
      emit(state.copyWith(
        currentStep: event.newStep,
        clearErrorMessage: true,
      ));
    }
  }

  void _onCategorySelected(
    CategorySelected event,
    Emitter<IncidentReportState> emit,
  ) {
    emit(state.copyWith(
      selectedCategory: event.category,
      clearErrorMessage: true,
    ));
  }

  void _onEmergencyCodeSelected(
    EmergencyCodeSelected event,
    Emitter<IncidentReportState> emit,
  ) {
    emit(state.copyWith(
      selectedEmergencyCode: event.code,
      clearErrorMessage: true,
    ));
  }

  void _onMediaFilesAdded(
    MediaFilesAdded event,
    Emitter<IncidentReportState> emit,
  ) {
    final updatedList = List.of(state.selectedMediaFiles)..addAll(event.files);
    emit(state.copyWith(
      selectedMediaFiles: updatedList,
      clearErrorMessage: true,
    ));
  }

  void _onMediaFileRemoved(
    MediaFileRemoved event,
    Emitter<IncidentReportState> emit,
  ) {
    if (event.index >= 0 && event.index < state.selectedMediaFiles.length) {
      final updatedList = List.of(state.selectedMediaFiles)..removeAt(event.index);
      emit(state.copyWith(
        selectedMediaFiles: updatedList,
        clearErrorMessage: true,
      ));
    }
  }

  Future<void> _onLocationRequested(
    LocationRequested event,
    Emitter<IncidentReportState> emit,
  ) async {
    emit(state.copyWith(
      isFetchingLocation: true,
      clearLocationError: true,
    ));

    try {
      final position = await _locationService.getCurrentLocation();
      emit(state.copyWith(
        currentPosition: position,
        isFetchingLocation: false,
        clearLocationError: true,
      ));
    } catch (e) {
      emit(state.copyWith(
        isFetchingLocation: false,
        locationError: e.toString().replaceAll('Exception: ', ''),
      ));
    }
  }

  void _onDescriptionChanged(
    DescriptionChanged event,
    Emitter<IncidentReportState> emit,
  ) {
    emit(state.copyWith(description: event.description));
  }

  Future<void> _onSubmitIncidentReportRequested(
    SubmitIncidentReportRequested event,
    Emitter<IncidentReportState> emit,
  ) async {
    if (!state.isStep1Valid) {
      emit(state.copyWith(
        errorMessage: 'Please select an incident category and emergency severity code.',
      ));
      return;
    }

    if (state.currentPosition == null) {
      emit(state.copyWith(
        errorMessage: 'GPS coordinates are required. Please acquire location before submitting.',
      ));
      return;
    }

    emit(state.copyWith(
      isSubmitting: true,
      clearErrorMessage: true,
      clearSubmissionSuccess: true,
      uploadProgressMessage: 'Preparing media attachments...',
    ));

    try {
      final List<CreateIncidentMediaItem> uploadedMediaItems = [];

      // 1. Upload local media files to MinIO
      final totalFiles = state.selectedMediaFiles.length;
      for (int i = 0; i < totalFiles; i++) {
        final mediaFile = state.selectedMediaFiles[i];
        emit(state.copyWith(
          uploadProgressMessage: 'Uploading media (${i + 1}/$totalFiles)...',
        ));

        final mediaUrl = await _incidentRepository.uploadIncidentMedia(mediaFile.file);
        uploadedMediaItems.add(
          CreateIncidentMediaItem(
            mediaUrl: mediaUrl,
            mediaType: mediaFile.mediaType,
          ),
        );
      }

      // 2. Dispatch incident report payload
      emit(state.copyWith(
        uploadProgressMessage: 'Submitting incident report...',
      ));

      final request = CreateIncidentRequestModel(
        category: state.selectedCategory!,
        emergencyCode: state.selectedEmergencyCode!.code,
        description: state.description.trim().isEmpty ? null : state.description.trim(),
        latitude: state.currentPosition!.latitude,
        longitude: state.currentPosition!.longitude,
        mediaAttachments: uploadedMediaItems,
      );

      final result = await _incidentRepository.createIncident(request);

      emit(state.copyWith(
        isSubmitting: false,
        clearUploadProgress: true,
        submissionSuccess: result,
      ));
    } catch (e) {
      emit(state.copyWith(
        isSubmitting: false,
        clearUploadProgress: true,
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      ));
    }
  }

  void _onResetWizardState(
    ResetWizardState event,
    Emitter<IncidentReportState> emit,
  ) {
    emit(IncidentReportState(
      emergencyCodes: state.emergencyCodes,
    ));
  }
}
