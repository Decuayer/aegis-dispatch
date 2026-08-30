import 'package:flutter/material.dart';
import '../../../../../core/constants/app_colors.dart';

class PermissionRationaleDialog extends StatelessWidget {
  final bool isPermanentlyDenied;
  final VoidCallback onAction;

  const PermissionRationaleDialog({
    super.key,
    required this.isPermanentlyDenied,
    required this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      backgroundColor: AppColors.surface,
      titlePadding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
      contentPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
      actionsPadding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      title: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppColors.accent.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.location_off_rounded,
              color: AppColors.accent,
              size: 24,
            ),
          ),
          const SizedBox(width: 12),
          const Expanded(
            child: Text(
              'Location Access Required',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
          ),
        ],
      ),
      content: Text(
        isPermanentlyDenied
            ? 'Location permissions have been permanently denied. To enable rapid emergency dispatch and responder telemetry, please allow location access in your device settings.'
            : 'SOCAR Dispatch requires location permissions to pinpoint emergency sites and dispatch nearest response teams. Please grant permissions to continue.',
        style: const TextStyle(
          fontSize: 13,
          height: 1.5,
          color: AppColors.textSecondary,
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text(
            'Cancel',
            style: TextStyle(color: AppColors.textSecondary),
          ),
        ),
        ElevatedButton(
          onPressed: () {
            Navigator.of(context).pop();
            onAction();
          },
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            minimumSize: const Size(120, 42),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
          child: Text(isPermanentlyDenied ? 'Open Settings' : 'Try Again'),
        ),
      ],
    );
  }
}
