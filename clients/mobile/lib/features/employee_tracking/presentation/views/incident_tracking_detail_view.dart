import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:latlong2/latlong.dart';
import '../../../../core/constants/app_colors.dart';
import '../bloc/employee_tracking_bloc.dart';
import '../bloc/employee_tracking_event.dart';
import '../bloc/employee_tracking_state.dart';
import '../widgets/assigned_team_info_card.dart';
import '../widgets/live_route_mini_map.dart';
import 'edit_incident_view.dart';

class IncidentTrackingDetailView extends StatefulWidget {
  final String incidentId;

  const IncidentTrackingDetailView({
    super.key,
    required this.incidentId,
  });

  @override
  State<IncidentTrackingDetailView> createState() => _IncidentTrackingDetailViewState();
}

class _IncidentTrackingDetailViewState extends State<IncidentTrackingDetailView> {
  @override
  void initState() {
    super.initState();
    context.read<EmployeeTrackingBloc>().add(
          SelectIncidentForTracking(widget.incidentId),
        );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Live Incident Tracking'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Refresh',
            onPressed: () {
              context.read<EmployeeTrackingBloc>().add(
                    SelectIncidentForTracking(widget.incidentId),
                  );
            },
          ),
        ],
      ),
      body: BlocConsumer<EmployeeTrackingBloc, EmployeeTrackingState>(
        listener: (context, state) {
          if (state is EmployeeTrackingLoaded) {
            if (state.updateSuccessMessage != null) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(state.updateSuccessMessage!),
                  backgroundColor: AppColors.success,
                ),
              );
              context.read<EmployeeTrackingBloc>().add(const ClearTrackingFeedback());
            } else if (state.updateErrorMessage != null) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(state.updateErrorMessage!),
                  backgroundColor: AppColors.error,
                ),
              );
              context.read<EmployeeTrackingBloc>().add(const ClearTrackingFeedback());
            }
          }
        },
        builder: (context, state) {
          if (state is! EmployeeTrackingLoaded || state.selectedIncident == null) {
            return const Center(child: CircularProgressIndicator());
          }

          final incident = state.selectedIncident!;
          final hasAssignedTeam = incident.assignedTeamName != null && incident.assignedTeamName!.isNotEmpty;

          return SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Top Meta Card
                Card(
                  elevation: 1,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              '#INC-${incident.id.substring(0, 8).toUpperCase()}',
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.bold,
                                color: AppColors.primary,
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: incident.isEditable
                                    ? AppColors.info.withValues(alpha: 0.15)
                                    : AppColors.success.withValues(alpha: 0.15),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Text(
                                incident.status,
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                  color: incident.isEditable ? AppColors.info : AppColors.success,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Text(
                          '${incident.category} Emergency',
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Emergency Code: ${incident.emergencyCode}',
                          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                        ),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 14),

                // Live Route Mini-Map
                LiveRouteMiniMap(
                  incidentLocation: LatLng(incident.latitude, incident.longitude),
                  teamLocation: incident.teamLatitude != null && incident.teamLongitude != null
                      ? LatLng(incident.teamLatitude!, incident.teamLongitude!)
                      : null,
                  routePoints: state.activeRoute,
                  distanceKm: state.distanceKm,
                  etaMinutes: state.etaMinutes,
                  isFallback: state.isRouteFallback,
                ),

                const SizedBox(height: 14),

                // Assigned Team Info Card
                if (hasAssignedTeam)
                  AssignedTeamInfoCard(
                    teamName: incident.assignedTeamName!,
                    leaderFullName: incident.assignedTeamLeaderName,
                    leaderPhone: incident.assignedTeamLeaderPhone,
                    memberCount: incident.assignedTeamMemberCount,
                    operationalStatus: incident.assignedTeamStatus,
                  )
                else
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surfaceMuted,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: const Row(
                      children: [
                        Icon(Icons.hourglass_empty_rounded, color: AppColors.warning),
                        SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            'Dispatch operator is assigning an emergency response unit...',
                            style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
                          ),
                        ),
                      ],
                    ),
                  ),

                const SizedBox(height: 14),

                // Situation Description
                Card(
                  elevation: 1,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Situational Description',
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          incident.description != null && incident.description!.isNotEmpty
                              ? incident.description!
                              : 'No description provided.',
                          style: const TextStyle(fontSize: 14, color: AppColors.textSecondary),
                        ),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 14),

                // Media Attachments
                if (incident.mediaAttachments.isNotEmpty) ...[
                  const Text(
                    'Attachments & Proof',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 8),
                  SizedBox(
                    height: 100,
                    child: ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: incident.mediaAttachments.length,
                      separatorBuilder: (_, __) => const SizedBox(width: 8),
                      itemBuilder: (context, index) {
                        final media = incident.mediaAttachments[index];
                        return ClipRRect(
                          borderRadius: BorderRadius.circular(10),
                          child: CachedNetworkImage(
                            imageUrl: media.mediaUrl,
                            width: 100,
                            height: 100,
                            fit: BoxFit.cover,
                            placeholder: (_, __) => Container(color: AppColors.surfaceMuted),
                            errorWidget: (_, __, ___) => Container(
                              color: AppColors.surfaceMuted,
                              child: const Icon(Icons.broken_image_rounded),
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                  const SizedBox(height: 16),
                ],

                // Edit Incident Button
                ElevatedButton.icon(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: incident.isEditable ? AppColors.primary : AppColors.surfaceMuted,
                    foregroundColor: incident.isEditable ? Colors.white : AppColors.textMuted,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                  ),
                  icon: const Icon(Icons.edit_note_rounded),
                  label: Text(
                    incident.isEditable
                        ? 'Edit Incident Details / Attachments'
                        : 'Incident Resolved — Editing Locked',
                    style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
                  ),
                  onPressed: incident.isEditable
                      ? () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => BlocProvider.value(
                                value: context.read<EmployeeTrackingBloc>(),
                                child: EditIncidentView(incident: incident),
                              ),
                            ),
                          );
                        }
                      : null,
                ),
                const SizedBox(height: 20),
              ],
            ),
          );
        },
      ),
    );
  }
}
