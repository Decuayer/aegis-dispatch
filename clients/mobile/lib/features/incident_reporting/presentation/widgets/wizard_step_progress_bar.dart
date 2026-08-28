import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';

class WizardStepProgressBar extends StatelessWidget {
  final int currentStep;

  const WizardStepProgressBar({
    super.key,
    required this.currentStep,
  });

  static const List<String> _stepTitles = [
    'Category & Code',
    'Media Evidence',
    'Location & Review',
  ];

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
      color: AppColors.surface,
      child: Row(
        children: List.generate(_stepTitles.length * 2 - 1, (index) {
          if (index.isOdd) {
            final stepBefore = index ~/ 2;
            final isPassed = currentStep > stepBefore;
            return Expanded(
              child: Container(
                height: 3,
                color: isPassed ? AppColors.secondary : AppColors.border,
              ),
            );
          }

          final stepIndex = index ~/ 2;
          final isCompleted = currentStep > stepIndex;
          final isCurrent = currentStep == stepIndex;

          Color badgeColor;
          Widget badgeContent;

          if (isCompleted) {
            badgeColor = AppColors.secondary;
            badgeContent = const Icon(Icons.check, size: 16, color: Colors.white);
          } else if (isCurrent) {
            badgeColor = AppColors.primary;
            badgeContent = Text(
              '${stepIndex + 1}',
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 13,
              ),
            );
          } else {
            badgeColor = AppColors.surfaceMuted;
            badgeContent = Text(
              '${stepIndex + 1}',
              style: const TextStyle(
                color: AppColors.textMuted,
                fontWeight: FontWeight.w600,
                fontSize: 13,
              ),
            );
          }

          return Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircleAvatar(
                radius: 14,
                backgroundColor: badgeColor,
                child: badgeContent,
              ),
              const SizedBox(height: 4),
              Text(
                _stepTitles[stepIndex],
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: isCurrent ? FontWeight.bold : FontWeight.normal,
                  color: isCurrent ? AppColors.primary : AppColors.textSecondary,
                ),
              ),
            ],
          );
        }),
      ),
    );
  }
}
