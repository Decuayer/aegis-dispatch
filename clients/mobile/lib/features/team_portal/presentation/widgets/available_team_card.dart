import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';
import '../../data/models/available_team_model.dart';

class AvailableTeamCard extends StatelessWidget {
  final AvailableTeamModel team;
  final bool isJoining;
  final VoidCallback onJoin;

  const AvailableTeamCard({
    super.key,
    required this.team,
    required this.isJoining,
    required this.onJoin,
  });

  @override
  Widget build(BuildContext context) {
    const maxCapacity = 6;
    final isFull = team.memberCount >= maxCapacity;
    final capacityRatio = (team.memberCount / maxCapacity).clamp(0.0, 1.0);

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(
                    Icons.shield_outlined,
                    color: AppColors.primary,
                    size: 24,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        team.teamName,
                        style: const TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Row(
                        children: [
                          Icon(
                            team.hasLeader
                                ? Icons.verified_user_outlined
                                : Icons.warning_amber_rounded,
                            size: 14,
                            color:
                                team.hasLeader
                                    ? AppColors.secondary
                                    : AppColors.warning,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            team.hasLeader
                                ? 'Leader: ${team.leaderFullName ?? "Designated"}'
                                : 'Leadership Vacant',
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight:
                                  team.hasLeader
                                      ? FontWeight.normal
                                      : FontWeight.w600,
                              color:
                                  team.hasLeader
                                      ? AppColors.textSecondary
                                      : AppColors.warning,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Unit Capacity',
                  style: TextStyle(
                    fontSize: 13,
                    color: AppColors.textSecondary,
                  ),
                ),
                Text(
                  '${team.memberCount} / $maxCapacity Members',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: isFull ? AppColors.error : AppColors.textPrimary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(
                value: capacityRatio,
                minHeight: 6,
                backgroundColor: AppColors.surfaceMuted,
                valueColor: AlwaysStoppedAnimation<Color>(
                  isFull ? AppColors.error : AppColors.secondary,
                ),
              ),
            ),
            const SizedBox(height: 14),
            SizedBox(
              width: double.infinity,
              height: 44,
              child: ElevatedButton.icon(
                onPressed: (isFull || isJoining) ? null : onJoin,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                  elevation: 0,
                ),
                icon:
                    isJoining
                        ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                        : const Icon(Icons.group_add_outlined, size: 20),
                label: Text(
                  isFull
                      ? 'Unit Full'
                      : (isJoining ? 'Joining...' : 'Join Team'),
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
