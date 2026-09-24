import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../../../core/constants/app_colors.dart';
import '../../bloc/incident_report_bloc.dart';
import '../../bloc/incident_report_event.dart';
import '../../bloc/incident_report_state.dart';
import '../../../services/location_service.dart';
import '../../widgets/gps_status_indicator.dart';

class StepLocationConfirm extends StatelessWidget {
  final LocationService locationService;

  const StepLocationConfirm({super.key, required this.locationService});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<IncidentReportBloc, IncidentReportState>(
      builder: (context, state) {
        Color badgeColor;
        try {
          final hex =
              state.selectedEmergencyCode?.colorHex.replaceFirst('#', '') ??
              'E30613';
          badgeColor = Color(int.parse('0xFF$hex'));
        } catch (_) {
          badgeColor = AppColors.accent;
        }

        return SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                '1. Real-Time GPS Location',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 10),
              GpsStatusIndicator(
                position: state.currentPosition,
                isFetching: state.isFetchingLocation,
                errorMessage: state.locationError,
                onRefresh: () {
                  context.read<IncidentReportBloc>().add(
                    const LocationRequested(),
                  );
                },
                onOpenSettings: () {
                  locationService.openAppSettings();
                },
              ),
              const SizedBox(height: 24),
              const Text(
                '2. Situational Notes (Optional)',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 10),
              TextFormField(
                maxLines: 3,
                initialValue: state.description,
                decoration: InputDecoration(
                  hintText: 'Describe details, hazards, or immediate risks...',
                  hintStyle: const TextStyle(
                    fontSize: 13,
                    color: AppColors.textMuted,
                  ),
                  filled: true,
                  fillColor: AppColors.surface,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: AppColors.border),
                  ),
                ),
                onChanged: (val) {
                  context.read<IncidentReportBloc>().add(
                    DescriptionChanged(val),
                  );
                },
              ),
              const SizedBox(height: 24),
              const Text(
                '3. Report Summary',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: AppColors.border),
                ),
                child: Column(
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Category:',
                          style: TextStyle(color: AppColors.textSecondary),
                        ),
                        Text(
                          state.selectedCategory ?? '-',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    const Divider(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Severity:',
                          style: TextStyle(color: AppColors.textSecondary),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 8,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: badgeColor.withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            state.selectedEmergencyCode != null
                                ? 'CODE ${state.selectedEmergencyCode!.code}'
                                : '-',
                            style: TextStyle(
                              color: badgeColor,
                              fontWeight: FontWeight.bold,
                              fontSize: 12,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const Divider(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Attached Media:',
                          style: TextStyle(color: AppColors.textSecondary),
                        ),
                        Text(
                          '${state.selectedMediaFiles.length} item(s)',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 28),
              ElevatedButton(
                onPressed:
                    state.canSubmit
                        ? () {
                          context.read<IncidentReportBloc>().add(
                            const SubmitIncidentReportRequested(),
                          );
                        }
                        : null,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.accent,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  elevation: 2,
                ),
                child:
                    state.isSubmitting
                        ? Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            ),
                            const SizedBox(width: 12),
                            Text(
                              state.uploadProgressMessage ?? 'Submitting...',
                              style: const TextStyle(
                                fontWeight: FontWeight.bold,
                                fontSize: 15,
                              ),
                            ),
                          ],
                        )
                        : const Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.warning_amber_rounded, size: 22),
                            SizedBox(width: 8),
                            Text(
                              'SUBMIT EMERGENCY REPORT',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                fontSize: 15,
                                letterSpacing: 0.5,
                              ),
                            ),
                          ],
                        ),
              ),
            ],
          ),
        );
      },
    );
  }
}
