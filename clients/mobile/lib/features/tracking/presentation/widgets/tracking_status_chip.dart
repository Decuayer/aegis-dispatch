import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';

class TrackingStatusChip extends StatefulWidget {
  final bool isTracking;
  final VoidCallback? onTap;

  const TrackingStatusChip({super.key, required this.isTracking, this.onTap});

  @override
  State<TrackingStatusChip> createState() => _TrackingStatusChipState();
}

class _TrackingStatusChipState extends State<TrackingStatusChip>
    with SingleTickerProviderStateMixin {
  late final AnimationController _pulseController;
  late final Animation<double> _pulseAnimation;

  @override
  void initState() {
    super.initState();
    _pulseController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1500),
    );

    _pulseAnimation = Tween<double>(begin: 0.3, end: 1.0).animate(
      CurvedAnimation(parent: _pulseController, curve: Curves.easeInOut),
    );

    if (widget.isTracking) {
      _pulseController.repeat(reverse: true);
    }
  }

  @override
  void didUpdateWidget(covariant TrackingStatusChip oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.isTracking != oldWidget.isTracking) {
      if (widget.isTracking) {
        _pulseController.repeat(reverse: true);
      } else {
        _pulseController.stop();
        _pulseController.reset();
      }
    }
  }

  @override
  void dispose() {
    _pulseController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final activeColor = AppColors.success;
    final inactiveColor = AppColors.textMuted;

    return InkWell(
      onTap: widget.onTap,
      borderRadius: BorderRadius.circular(20),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 300),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        decoration: BoxDecoration(
          color:
              widget.isTracking
                  ? activeColor.withValues(alpha: 0.12)
                  : AppColors.surfaceMuted,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color:
                widget.isTracking
                    ? activeColor.withValues(alpha: 0.35)
                    : AppColors.border,
            width: 1,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            widget.isTracking
                ? FadeTransition(
                  opacity: _pulseAnimation,
                  child: Container(
                    width: 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: activeColor,
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: activeColor.withValues(alpha: 0.6),
                          blurRadius: 4,
                          spreadRadius: 1,
                        ),
                      ],
                    ),
                  ),
                )
                : Container(
                  width: 8,
                  height: 8,
                  decoration: BoxDecoration(
                    color: inactiveColor,
                    shape: BoxShape.circle,
                  ),
                ),
            const SizedBox(width: 6),
            Text(
              widget.isTracking ? 'Live GPS Active' : 'GPS Standby',
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w600,
                color: widget.isTracking ? activeColor : inactiveColor,
                letterSpacing: 0.2,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
