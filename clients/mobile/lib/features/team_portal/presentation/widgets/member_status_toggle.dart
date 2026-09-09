import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../team_tasks/data/models/team_model.dart';

class MemberStatusToggle extends StatelessWidget {
  final MemberStatus currentStatus;
  final ValueChanged<MemberStatus> onStatusChanged;

  const MemberStatusToggle({
    super.key,
    required this.currentStatus,
    required this.onStatusChanged,
  });

  @override
  Widget build(BuildContext context) {
    final isAvailable = currentStatus == MemberStatus.available;

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: (isAvailable
                          ? AppColors.secondary
                          : AppColors.textMuted)
                      .withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(
                  isAvailable
                      ? Icons.radio_button_checked
                      : Icons.do_not_disturb_on_outlined,
                  color:
                      isAvailable ? AppColors.secondary : AppColors.textMuted,
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Personal Duty Status',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  Text(
                    isAvailable
                        ? 'Ready to receive emergency dispatches'
                        : 'Off-duty / Unavailable',
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ],
          ),
          Switch.adaptive(
            value: isAvailable,
            activeTrackColor: AppColors.secondary,
            onChanged: (val) {
              onStatusChanged(
                val ? MemberStatus.available : MemberStatus.offDuty,
              );
            },
          ),
        ],
      ),
    );
  }
}
