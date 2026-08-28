import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../data/models/incident_response_model.dart';
import '../bloc/incident_report_bloc.dart';
import '../bloc/incident_report_event.dart';
import '../bloc/incident_report_state.dart';
import '../../services/location_service.dart';
import '../../services/media_picker_service.dart';
import '../widgets/wizard_step_progress_bar.dart';
import 'steps/step_category_severity.dart';
import 'steps/step_location_confirm.dart';
import 'steps/step_media_capture.dart';

class IncidentReportWizardView extends StatefulWidget {
  const IncidentReportWizardView({super.key});

  @override
  State<IncidentReportWizardView> createState() => _IncidentReportWizardViewState();
}

class _IncidentReportWizardViewState extends State<IncidentReportWizardView> {
  @override
  void initState() {
    super.initState();
    final bloc = context.read<IncidentReportBloc>();
    bloc.add(const LoadEmergencyCodesStarted());
    bloc.add(const LocationRequested());
  }

  void _showSuccessDialog(BuildContext context, IncidentResponseModel incident) {
    final refId = incident.id.length >= 8
        ? incident.id.substring(0, 8).toUpperCase()
        : incident.id.toUpperCase();

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (dialogCtx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        contentPadding: const EdgeInsets.all(24),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppColors.secondary.withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.check_circle_rounded,
                color: AppColors.secondary,
                size: 56,
              ),
            ),
            const SizedBox(height: 18),
            const Text(
              'Incident Dispatched',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Incident #$refId has been registered. Dispatch control room and emergency response units have been alerted.',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.textSecondary,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 18),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: AppColors.surfaceMuted,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppColors.border),
              ),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Status:',
                        style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          incident.status.toUpperCase(),
                          style: const TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Category:',
                        style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
                      ),
                      Text(
                        incident.category,
                        style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () {
                  Navigator.of(dialogCtx).pop();
                  context.read<IncidentReportBloc>().add(const ResetWizardState());
                  Navigator.of(context).pop();
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                child: const Text(
                  'Return to Dashboard',
                  style: TextStyle(fontWeight: FontWeight.bold),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showLocationPermissionDialog(BuildContext context, LocationService locationService) {
    showDialog(
      context: context,
      builder: (dialogCtx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.location_disabled_rounded, color: AppColors.error, size: 24),
            SizedBox(width: 10),
            Text('GPS Access Required'),
          ],
        ),
        content: const Text(
          'Accurate GPS coordinates are required to pinpoint plant hazards for emergency teams. Please grant location access in device settings.',
          style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogCtx).pop(),
            child: const Text('Cancel'),
          ),
          ElevatedButton.icon(
            onPressed: () {
              Navigator.of(dialogCtx).pop();
              locationService.openAppSettings();
            },
            icon: const Icon(Icons.settings, size: 16),
            label: const Text('Open Settings'),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final locationService = RepositoryProvider.of<LocationService>(context);
    final mediaPickerService = RepositoryProvider.of<MediaPickerService>(context);

    return BlocConsumer<IncidentReportBloc, IncidentReportState>(
      listenWhen: (prev, current) =>
          prev.errorMessage != current.errorMessage ||
          prev.submissionSuccess != current.submissionSuccess ||
          prev.locationError != current.locationError,
      listener: (context, state) {
        if (state.errorMessage != null) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Row(
                children: [
                  const Icon(Icons.error_outline, color: Colors.white, size: 20),
                  const SizedBox(width: 10),
                  Expanded(child: Text(state.errorMessage!)),
                ],
              ),
              backgroundColor: AppColors.error,
              behavior: SnackBarBehavior.floating,
              duration: const Duration(seconds: 4),
            ),
          );
        }

        if (state.locationError != null &&
            state.locationError!.toLowerCase().contains('denied')) {
          _showLocationPermissionDialog(context, locationService);
        }

        if (state.submissionSuccess != null) {
          _showSuccessDialog(context, state.submissionSuccess!);
        }
      },
      builder: (context, state) {
        return Scaffold(
          backgroundColor: AppColors.background,
          appBar: AppBar(
            title: const Text('Report Emergency Incident'),
            leading: IconButton(
              icon: const Icon(Icons.arrow_back_ios_rounded, size: 20),
              onPressed: () {
                if (state.currentStep > 0) {
                  context
                      .read<IncidentReportBloc>()
                      .add(StepChanged(state.currentStep - 1));
                } else {
                  Navigator.of(context).pop();
                }
              },
            ),
          ),
          body: Column(
            children: [
              WizardStepProgressBar(currentStep: state.currentStep),
              Expanded(
                child: IndexedStack(
                  index: state.currentStep,
                  children: [
                    const StepCategorySeverity(),
                    StepMediaCapture(mediaPickerService: mediaPickerService),
                    StepLocationConfirm(locationService: locationService),
                  ],
                ),
              ),
            ],
          ),
          bottomNavigationBar: state.currentStep < 2
              ? Container(
                  padding: const EdgeInsets.all(16),
                  decoration: const BoxDecoration(
                    color: AppColors.surface,
                    border: Border(top: BorderSide(color: AppColors.border)),
                  ),
                  child: Row(
                    children: [
                      if (state.currentStep > 0)
                        Expanded(
                          child: OutlinedButton(
                            onPressed: () {
                              context
                                  .read<IncidentReportBloc>()
                                  .add(StepChanged(state.currentStep - 1));
                            },
                            style: OutlinedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(vertical: 14),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                            ),
                            child: const Text('Back'),
                          ),
                        ),
                      if (state.currentStep > 0) const SizedBox(width: 12),
                      Expanded(
                        flex: 2,
                        child: ElevatedButton(
                          onPressed: () {
                            if (state.currentStep == 0 && !state.isStep1Valid) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text(
                                    'Please select a category and an emergency severity code.',
                                  ),
                                  behavior: SnackBarBehavior.floating,
                                ),
                              );
                              return;
                            }
                            context
                                .read<IncidentReportBloc>()
                                .add(StepChanged(state.currentStep + 1));
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(vertical: 14),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                          ),
                          child: const Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Text('Continue', style: TextStyle(fontWeight: FontWeight.bold)),
                              SizedBox(width: 6),
                              Icon(Icons.arrow_forward_rounded, size: 18),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                )
              : null,
        );
      },
    );
  }
}
