import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/widgets/entity_id_badge.dart';

class AssignedTeamInfoCard extends StatelessWidget {
  final String teamName;
  final String? teamId;
  final String? leaderFullName;
  final String? leaderPhone;
  final int? memberCount;
  final String? operationalStatus; // Forwarded, EnRoute, OnScene, Busy

  const AssignedTeamInfoCard({
    super.key,
    required this.teamName,
    this.teamId, // <-- EKLENDİ
    this.leaderFullName,
    this.leaderPhone,
    this.memberCount,
    this.operationalStatus,
  });

  Color _getStatusColor(String? status) {
    switch (status?.toLowerCase()) {
      case 'onscene':
      case 'on_scene':
        return AppColors.success;
      case 'enroute':
      case 'in_transit':
        return AppColors.warning;
      case 'forwarded':
      case 'assigned':
        return AppColors.info;
      default:
        return AppColors.textSecondary;
    }
  }

  String _formatStatusLabel(String? status) {
    switch (status?.toLowerCase()) {
      case 'onscene':
      case 'on_scene':
        return 'On Scene';
      case 'enroute':
      case 'in_transit':
        return 'En Route';
      case 'forwarded':
      case 'assigned':
        return 'Forwarded';
      default:
        return status ?? 'Assigned';
    }
  }

  Future<void> _makePhoneCall(BuildContext context, String phone) async {
    final cleanPhone = phone.replaceAll(' ', '');
    final uri = Uri(scheme: 'tel', path: cleanPhone);
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri);
    } else {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Could not initiate call to $phone'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final statusColor = _getStatusColor(operationalStatus);
    final statusLabel = _formatStatusLabel(operationalStatus);

    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: AppColors.border, width: 1),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header Row: Team Name & Status Badge
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(8),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.1),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          Icons.shield_outlined,
                          color: AppColors.primary,
                          size: 20,
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(
                          teamName,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      if (teamId != null && teamId!.isNotEmpty) ...[
                        const SizedBox(width: 8),
                        EntityIdBadge(
                          id: teamId!,
                          type: EntityBadgeType.team,
                          isCompact: true,
                        ),
                      ],
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    statusLabel,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: statusColor,
                    ),
                  ),
                ),
              ],
            ),

            const Divider(height: 24),

            // Leader & Members Row
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Team Leader: ${leaderFullName ?? "Designated Officer"}',
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      memberCount != null
                          ? '$memberCount Responders'
                          : 'Field Crew',
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),

                // Direct Call Button
                if (leaderPhone != null && leaderPhone!.isNotEmpty)
                  ElevatedButton.icon(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.secondary,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 14,
                        vertical: 8,
                      ),
                    ),
                    icon: const Icon(Icons.phone_in_talk_rounded, size: 16),
                    label: const Text(
                      'Call',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    onPressed: () => _makePhoneCall(context, leaderPhone!),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
