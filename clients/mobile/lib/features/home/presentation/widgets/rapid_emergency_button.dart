import 'package:flutter/material.dart';
import '../../../incident_reporting/data/models/rapid_emergency_preset.dart';

class RapidEmergencyButton extends StatelessWidget {
  final RapidEmergencyPreset preset;
  final bool isLoading;
  final bool isDisabled;
  final VoidCallback onTap;

  const RapidEmergencyButton({
    super.key,
    required this.preset,
    this.isLoading = false,
    this.isDisabled = false,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final opacity = isDisabled ? 0.45 : 1.0;

    return AnimatedOpacity(
      duration: const Duration(milliseconds: 200),
      opacity: opacity,
      child: Material(
        color: preset.backgroundColor,
        borderRadius: BorderRadius.circular(16),
        elevation: isDisabled ? 0 : 2,
        shadowColor: preset.primaryColor.withValues(alpha: 0.2),
        child: InkWell(
          onTap: (isDisabled || isLoading) ? null : onTap,
          borderRadius: BorderRadius.circular(16),
          splashColor: preset.primaryColor.withValues(alpha: 0.15),
          highlightColor: preset.primaryColor.withValues(alpha: 0.08),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: preset.primaryColor.withValues(alpha: 0.35),
                width: 1.5,
              ),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: preset.primaryColor.withValues(alpha: 0.15),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        preset.icon,
                        color: preset.primaryColor,
                        size: 28,
                      ),
                    ),
                    if (isLoading)
                      SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(
                          strokeWidth: 2.5,
                          valueColor: AlwaysStoppedAnimation<Color>(preset.primaryColor),
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 12),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      preset.title,
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: preset.primaryColor,
                        letterSpacing: -0.2,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      preset.subtitle,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                        color: Colors.black.withValues(alpha: 0.65),
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
