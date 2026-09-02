import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';
import '../../data/models/tracked_incident_model.dart';
import '../../../../core/widgets/entity_id_badge.dart';


class ActiveIncidentCard extends StatelessWidget {
  final TrackedIncidentModel incident;
  final VoidCallback onTap;

  const ActiveIncidentCard({
    super.key,
    required this.incident,
    required this.onTap,
  });

  Color _getEmergencyCodeColor(String code) {
    final lower = code.toLowerCase();
    if (lower.contains('red')) return AppColors.accent;
    if (lower.contains('yellow') || lower.contains('amber')) return AppColors.warning;
    if (lower.contains('blue')) return AppColors.info;
    return AppColors.primary;
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'open':
        return AppColors.info;
      case 'assigned':
      case 'forwarded':
        return AppColors.warning;
      case 'inprogress':
      case 'in_progress':
      case 'enroute':
      case 'onscene':
        return const Color(0xFF7C3AED); // Purple
      case 'resolved':
        return AppColors.success;
      default:
        return AppColors.textMuted;
    }
  }

  IconData _getCategoryIcon(String category) {
    switch (category.toLowerCase()) {
      case 'fire':
        return Icons.local_fire_department_rounded;
      case 'gas':
      case 'chemical':
        return Icons.warning_amber_rounded;
      case 'medical':
        return Icons.medical_services_rounded;
      case 'security':
        return Icons.shield_rounded;
      default:
        return Icons.emergency_rounded;
    }
  }

  String _formatElapsedTime(Duration duration) {
    if (duration.inMinutes < 1) return 'Just now';
    if (duration.inMinutes < 60) return '${duration.inMinutes}m ago';
    if (duration.inHours < 24) return '${duration.inHours}h ago';
    return '${duration.inDays}d ago';
  }

  @override
  Widget build(BuildContext context) {
    final codeColor = _getEmergencyCodeColor(incident.emergencyCode);
    final statusColor = _getStatusColor(incident.status);
    final categoryIcon = _getCategoryIcon(incident.category);
    final elapsedText = _formatElapsedTime(incident.durationSinceDispatch);

    return Card(
      elevation: 2,
      margin: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: AppColors.border, width: 1),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top Badges Row
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      // Emergency Code Pill
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: codeColor.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: codeColor.withValues(alpha: 0.4)),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(categoryIcon, size: 14, color: codeColor),
                            const SizedBox(width: 4),
                            Text(
                              incident.emergencyCode,
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: codeColor,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),
                      EntityIdBadge(
                        id: incident.id,
                        type: EntityBadgeType.incident,
                        isCompact: true,
                      ),
                    ],
                  ),

                  // Status Pill
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: statusColor.withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      incident.status,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                        color: statusColor,
                      ),
                    ),
                  ),
                ],
              ),

              const SizedBox(height: 10),

              // Title / Category
              Text(
                '${incident.category} Incident',
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),

              const SizedBox(height: 4),

              // Description snippet
              if (incident.description != null && incident.description!.isNotEmpty)
                Text(
                  incident.description!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 13,
                    color: AppColors.textSecondary,
                  ),
                ),

              const SizedBox(height: 10),

              // Bottom Info: Assigned Team & Elapsed Timer
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Icon(
                        incident.assignedTeamName != null
                            ? Icons.group_outlined
                            : Icons.hourglass_top_rounded,
                        size: 14,
                        color: AppColors.textMuted,
                      ),
                      const SizedBox(width: 4),
                      Text(
                        incident.assignedTeamName ?? 'Awaiting Team',
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                  Row(
                    children: [
                      const Icon(Icons.schedule_rounded, size: 13, color: AppColors.textMuted),
                      const SizedBox(width: 3),
                      Text(
                        elapsedText,
                        style: const TextStyle(
                          fontSize: 11,
                          color: AppColors.textMuted,
                        ),
                      ),
                      const SizedBox(width: 4),
                      const Icon(Icons.chevron_right_rounded, size: 16, color: AppColors.textMuted),
                    ],
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
