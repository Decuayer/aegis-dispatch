import 'package:geolocator/geolocator.dart';
import '../../data/models/emergency_code_model.dart';
import '../../data/models/incident_response_model.dart';
import '../../services/media_picker_service.dart';

class IncidentReportState {
  final int currentStep;
  final List<EmergencyCodeModel> emergencyCodes;
  final bool isLoadingCodes;
  final String? selectedCategory;
  final EmergencyCodeModel? selectedEmergencyCode;
  final List<SelectedMediaFile> selectedMediaFiles;
  final Position? currentPosition;
  final bool isFetchingLocation;
  final String? locationError;
  final String description;
  final bool isSubmitting;
  final String? uploadProgressMessage;
  final IncidentResponseModel? submissionSuccess;
  final String? errorMessage;

  const IncidentReportState({
    this.currentStep = 0,
    this.emergencyCodes = const [],
    this.isLoadingCodes = false,
    this.selectedCategory,
    this.selectedEmergencyCode,
    this.selectedMediaFiles = const [],
    this.currentPosition,
    this.isFetchingLocation = false,
    this.locationError,
    this.description = '',
    this.isSubmitting = false,
    this.uploadProgressMessage,
    this.submissionSuccess,
    this.errorMessage,
  });

  bool get isStep1Valid =>
      selectedCategory != null &&
      selectedCategory!.isNotEmpty &&
      selectedEmergencyCode != null;

  bool get isStep2Valid => true;

  bool get isStep3Valid => currentPosition != null;

  bool get canSubmit => isStep1Valid && isStep3Valid && !isSubmitting;

  IncidentReportState copyWith({
    int? currentStep,
    List<EmergencyCodeModel>? emergencyCodes,
    bool? isLoadingCodes,
    String? selectedCategory,
    EmergencyCodeModel? selectedEmergencyCode,
    List<SelectedMediaFile>? selectedMediaFiles,
    Position? currentPosition,
    bool? isFetchingLocation,
    String? locationError,
    bool clearLocationError = false,
    String? description,
    bool? isSubmitting,
    String? uploadProgressMessage,
    bool clearUploadProgress = false,
    IncidentResponseModel? submissionSuccess,
    bool clearSubmissionSuccess = false,
    String? errorMessage,
    bool clearErrorMessage = false,
  }) {
    return IncidentReportState(
      currentStep: currentStep ?? this.currentStep,
      emergencyCodes: emergencyCodes ?? this.emergencyCodes,
      isLoadingCodes: isLoadingCodes ?? this.isLoadingCodes,
      selectedCategory: selectedCategory ?? this.selectedCategory,
      selectedEmergencyCode:
          selectedEmergencyCode ?? this.selectedEmergencyCode,
      selectedMediaFiles: selectedMediaFiles ?? this.selectedMediaFiles,
      currentPosition: currentPosition ?? this.currentPosition,
      isFetchingLocation: isFetchingLocation ?? this.isFetchingLocation,
      locationError:
          clearLocationError ? null : (locationError ?? this.locationError),
      description: description ?? this.description,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      uploadProgressMessage:
          clearUploadProgress
              ? null
              : (uploadProgressMessage ?? this.uploadProgressMessage),
      submissionSuccess:
          clearSubmissionSuccess
              ? null
              : (submissionSuccess ?? this.submissionSuccess),
      errorMessage:
          clearErrorMessage ? null : (errorMessage ?? this.errorMessage),
    );
  }
}
