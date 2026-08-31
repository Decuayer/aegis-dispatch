import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../team_tasks/data/models/team_model.dart';

class TeamRosterTile extends StatelessWidget {
  final TeamMemberModel member;
  final bool isLeader;
  final bool isCurrentUser;
  final bool canManage;
  final VoidCallback? onRemove;

  const TeamRosterTile({
    super.key,
    required this.member,
    required this.isLeader,
    required this.isCurrentUser,
    required this.canManage,
    this.onRemove,
  });

  String _getInitials(String name) {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.isNotEmpty ? name[0].toUpperCase() : '?';
  }

  Color _getStatusColor(MemberStatus status) {
    switch (status) {
      case MemberStatus.available:
        return AppColors.secondary;
      case MemberStatus.enRoute:
      case MemberStatus.onScene:
        return AppColors.info;
      case MemberStatus.busy:
      case MemberStatus.offDuty:
        return AppColors.textMuted;
    }
  }

  void _showRemoveConfirmation(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Remove Team Member'),
        content: Text('Are you sure you want to remove ${member.fullName} from the team roster?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () {
              Navigator.pop(ctx);
              onRemove?.call();
            },
            style: FilledButton.styleFrom(backgroundColor: AppColors.error),
            child: const Text('Remove'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final statusColor = _getStatusColor(member.memberStatus);

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isCurrentUser ? AppColors.primary.withValues(alpha: 0.3) : AppColors.border,
        ),
      ),
      child: Row(
        children: [
          CircleAvatar(
            radius: 20,
            backgroundColor: isLeader
                ? AppColors.warning.withValues(alpha: 0.15)
                : AppColors.primary.withValues(alpha: 0.1),
            child: Text(
              _getInitials(member.fullName),
              style: TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 14,
                color: isLeader ? AppColors.warning : AppColors.primary,
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        member.fullName,
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    if (isLeader) ...[
                      const SizedBox(width: 6),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: AppColors.warning.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.star, size: 10, color: AppColors.warning),
                            SizedBox(width: 2),
                            Text(
                              'LEADER',
                              style: TextStyle(
                                fontSize: 9,
                                fontWeight: FontWeight.bold,
                                color: AppColors.warning,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                    if (isCurrentUser) ...[
                      const SizedBox(width: 6),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: const Text(
                          'YOU',
                          style: TextStyle(
                            fontSize: 9,
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  member.subRole ?? member.department,
                  style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
                ),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: statusColor.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 6,
                  height: 6,
                  decoration: BoxDecoration(color: statusColor, shape: BoxShape.circle),
                ),
                const SizedBox(width: 4),
                Text(
                  member.memberStatus.label,
                  style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: statusColor),
                ),
              ],
            ),
          ),
          if (canManage && !isLeader && !isCurrentUser) ...[
            const SizedBox(width: 4),
            IconButton(
              icon: const Icon(Icons.person_remove_outlined, size: 20, color: AppColors.error),
              tooltip: 'Remove from team',
              onPressed: () => _showRemoveConfirmation(context),
            ),
          ],
        ],
      ),
    );
  }
}
