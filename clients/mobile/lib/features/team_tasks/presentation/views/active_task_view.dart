import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart';
import '../../../../core/constants/app_colors.dart';
import '../../data/models/task_detail_model.dart';
import '../../data/models/team_model.dart';
import '../widgets/completion_notes_modal.dart';
import '../widgets/incident_media_carousel.dart';
import '../widgets/reporter_contact_card.dart';
import '../widgets/status_action_bar.dart';
import 'task_route_map_view.dart';
import '../../../../core/widgets/entity_id_badge.dart';

class ActiveTaskView extends StatelessWidget {
  final TeamTaskModel task;
  final List<LatLng> routePoints;
  final double distanceKm;
  final int estimatedMinutes;
  final LatLng? teamLocation;
  final bool isRouteFallback;
  final bool isStatusUpdating;
  final ValueChanged<TeamStatus> onStatusChange;
  final Future<void> Function(String notes, dynamic photo) onDebriefSubmit;

  const ActiveTaskView({
    super.key,
    required this.task,
    required this.routePoints,
    required this.distanceKm,
    required this.estimatedMinutes,
    this.teamLocation,
    this.isRouteFallback = false,
    this.isStatusUpdating = false,
    required this.onStatusChange,
    required this.onDebriefSubmit,
  });

  @override
  Widget build(BuildContext context) {
    final currentStatus = TeamStatus.fromString(task.status);
    final fallbackCoords = LatLng(
      task.latitude - 0.005,
      task.longitude - 0.005,
    );
    final effectiveTeamLocation = teamLocation ?? fallbackCoords;

    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _buildHeaderBadge(currentStatus),
                const SizedBox(height: 14),
                TaskRouteMapView(
                  teamLocation: effectiveTeamLocation,
                  incidentLocation: LatLng(task.latitude, task.longitude),
                  routePoints: routePoints,
                  distanceKm: distanceKm,
                  estimatedMinutes: estimatedMinutes,
                  isRouteFallback: isRouteFallback,
                  incidentId: task.id,
                  teamId: task.assignedTeamId,
                ),
                const SizedBox(height: 16),
                _buildIncidentDetailsCard(),
                const SizedBox(height: 14),
                if (task.mediaAttachments.isNotEmpty) ...[
                  IncidentMediaCarousel(mediaList: task.mediaAttachments),
                  const SizedBox(height: 14),
                ],
                ReporterContactCard(
                  reporterName: task.reporterFullName,
                  reporterPhone: task.reporterPhone,
                  reporterDepartment: task.reporterDepartment,
                  reporterAvatarUrl: task.reporterAvatarUrl,
                ),
                const SizedBox(height: 16),
              ],
            ),
          ),
        ),
        StatusActionBar(
          currentStatus: currentStatus,
          isLoading: isStatusUpdating,
          onStatusChangeRequested: onStatusChange,
          onResolveRequested:
              () => CompletionNotesModal.show(
                context,
                onSubmit: (notes, photo) => onDebriefSubmit(notes, photo),
              ),
        ),
      ],
    );
  }

  Widget _buildHeaderBadge(TeamStatus status) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color: AppColors.accent,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      task.emergencyCode,
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                        fontSize: 13,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    task.category,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                ],
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: status.color.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  status.label,
                  style: TextStyle(
                    color: status.color,
                    fontWeight: FontWeight.bold,
                    fontSize: 12,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              EntityIdBadge(id: task.id, type: EntityBadgeType.incident),
              if (task.assignedTeamId != null &&
                  task.assignedTeamId!.isNotEmpty) ...[
                const SizedBox(width: 8),
                EntityIdBadge(
                  id: task.assignedTeamId!,
                  type: EntityBadgeType.team,
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildIncidentDetailsCard() {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Incident Description',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontSize: 14,
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            task.description?.isNotEmpty == true
                ? task.description!
                : 'No additional description provided by reporter.',
            style: const TextStyle(fontSize: 14, height: 1.4),
          ),
        ],
      ),
    );
  }
}
