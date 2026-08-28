import '../../data/models/emergency_code_model.dart';
import '../../services/media_picker_service.dart';

abstract class IncidentReportEvent {
  const IncidentReportEvent();
}

class LoadEmergencyCodesStarted extends IncidentReportEvent {
  const LoadEmergencyCodesStarted();
}

class StepChanged extends IncidentReportEvent {
  final int newStep;
  const StepChanged(this.newStep);
}

class CategorySelected extends IncidentReportEvent {
  final String category;
  const CategorySelected(this.category);
}

class EmergencyCodeSelected extends IncidentReportEvent {
  final EmergencyCodeModel code;
  const EmergencyCodeSelected(this.code);
}

class MediaFilesAdded extends IncidentReportEvent {
  final List<SelectedMediaFile> files;
  const MediaFilesAdded(this.files);
}

class MediaFileRemoved extends IncidentReportEvent {
  final int index;
  const MediaFileRemoved(this.index);
}

class LocationRequested extends IncidentReportEvent {
  const LocationRequested();
}

class DescriptionChanged extends IncidentReportEvent {
  final String description;
  const DescriptionChanged(this.description);
}

class SubmitIncidentReportRequested extends IncidentReportEvent {
  const SubmitIncidentReportRequested();
}

class ResetWizardState extends IncidentReportEvent {
  const ResetWizardState();
}
