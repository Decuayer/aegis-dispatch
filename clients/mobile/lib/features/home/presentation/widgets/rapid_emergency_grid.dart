import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../incident_reporting/data/models/rapid_emergency_preset.dart';
import '../../../incident_reporting/presentation/bloc/rapid_incident_cubit.dart';
import '../../../incident_reporting/presentation/bloc/rapid_incident_state.dart';
import 'rapid_emergency_button.dart';

class RapidEmergencyGrid extends StatelessWidget {
  const RapidEmergencyGrid({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<RapidIncidentCubit, RapidIncidentState>(
      builder: (context, state) {
        final isSubmitting = state is RapidIncidentSubmitting;
        final activePresetId = isSubmitting ? state.activePresetId : null;

        return GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 2,
            crossAxisSpacing: 12,
            mainAxisSpacing: 12,
            childAspectRatio: 1.15,
          ),
          itemCount: RapidEmergencyPreset.presets.length,
          itemBuilder: (context, index) {
            final preset = RapidEmergencyPreset.presets[index];
            final isLoading = isSubmitting && activePresetId == preset.id;
            final isDisabled = isSubmitting && activePresetId != preset.id;

            return RapidEmergencyButton(
              preset: preset,
              isLoading: isLoading,
              isDisabled: isDisabled,
              onTap: () {
                context.read<RapidIncidentCubit>().triggerRapidIncident(preset);
              },
            );
          },
        );
      },
    );
  }
}
