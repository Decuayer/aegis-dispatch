import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../../../core/constants/app_colors.dart';
import '../../data/models/team_model.dart';

class StatusActionBar extends StatelessWidget {
  final TeamStatus currentStatus;
  final bool isLoading;
  final ValueChanged<TeamStatus> onStatusChangeRequested;
  final VoidCallback onResolveRequested;

  const StatusActionBar({
    super.key,
    required this.currentStatus,
    required this.isLoading,
    required this.onStatusChangeRequested,
    required this.onResolveRequested,
  });

  @override
  Widget build(BuildContext context) {
    if (currentStatus == TeamStatus.idle || currentStatus == TeamStatus.resolved) {
      return const SizedBox.shrink();
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.08),
            offset: const Offset(0, -4),
            blurRadius: 16,
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: _buildActionButton(context),
      ),
    );
  }

  Widget _buildActionButton(BuildContext context) {
    String label;
    IconData icon;
    Color buttonColor;
    VoidCallback onPressed;

    switch (currentStatus) {
      case TeamStatus.forwarded:
        label = 'Depart Now (En Route)';
        icon = Icons.directions_car_filled_outlined;
        buttonColor = AppColors.warning;
        onPressed = () {
          HapticFeedback.mediumImpact();
          onStatusChangeRequested(TeamStatus.enRoute);
        };
        break;

      case TeamStatus.enRoute:
        label = 'Confirm On Scene';
        icon = Icons.place_rounded;
        buttonColor = AppColors.accent;
        onPressed = () {
          HapticFeedback.mediumImpact();
          onStatusChangeRequested(TeamStatus.onScene);
        };
        break;

      case TeamStatus.onScene:
        label = 'Complete Task & Debrief';
        icon = Icons.task_alt_rounded;
        buttonColor = AppColors.secondary;
        onPressed = () {
          HapticFeedback.mediumImpact();
          onResolveRequested();
        };
        break;

      default:
        return const SizedBox.shrink();
    }

    return SizedBox(
      height: 52,
      child: ElevatedButton(
        style: ElevatedButton.styleFrom(
          backgroundColor: buttonColor,
          foregroundColor: Colors.white,
          elevation: 2,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
        onPressed: isLoading ? null : onPressed,
        child: isLoading
            ? const SizedBox(
                height: 24,
                width: 24,
                child: CircularProgressIndicator(
                  strokeWidth: 2.5,
                  valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                ),
              )
            : Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(icon, size: 22),
                  const SizedBox(width: 10),
                  Text(
                    label,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 0.3,
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}
