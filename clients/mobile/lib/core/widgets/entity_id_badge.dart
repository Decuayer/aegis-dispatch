import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../constants/app_colors.dart';

enum EntityBadgeType { incident, team }

class EntityIdBadge extends StatelessWidget {
  final String id;
  final EntityBadgeType type;
  final bool isCompact;
  final bool isCopyable;

  const EntityIdBadge({
    super.key,
    required this.id,
    required this.type,
    this.isCompact = false,
    this.isCopyable = true,
  });

  /// Formats raw UUID or ID strings into canonical badges (#INC-XXXX or #TEAM-XX)
  String get formattedDisplayId {
    final cleanId = id.trim();
    if (cleanId.isEmpty) {
      return type == EntityBadgeType.incident ? '#INC-N/A' : '#TEAM-N/A';
    }

    if (cleanId.startsWith('#')) {
      return cleanId.toUpperCase();
    }

    final prefix = type == EntityBadgeType.incident ? 'INC' : 'TEAM';
    if (cleanId.toUpperCase().startsWith('$prefix-')) {
      return '#${cleanId.toUpperCase()}';
    }

    final rawKey = cleanId.replaceAll('-', '').toUpperCase();
    final short = rawKey.length > 4 ? rawKey.substring(0, 4) : rawKey;
    return '#$prefix-$short';
  }

  void _copyToClipboard(BuildContext context) {
    if (!isCopyable) return;

    Clipboard.setData(ClipboardData(text: id));
    HapticFeedback.lightImpact();

    final label = type == EntityBadgeType.incident ? 'Incident ID' : 'Team ID';
    ScaffoldMessenger.of(context).hideCurrentSnackBar();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            const Icon(Icons.check_circle_outline, color: Colors.white, size: 18),
            const SizedBox(width: 8),
            Text('$label copied to clipboard: $formattedDisplayId'),
          ],
        ),
        backgroundColor: AppColors.primary,
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isIncident = type == EntityBadgeType.incident;
    final badgeColor = isIncident ? AppColors.accent : AppColors.primary;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: isCopyable ? () => _copyToClipboard(context) : null,
        borderRadius: BorderRadius.circular(isCompact ? 6 : 8),
        child: Container(
          padding: EdgeInsets.symmetric(
            horizontal: isCompact ? 4 : 8,
            vertical: isCompact ? 1.5 : 4,
          ),
          decoration: BoxDecoration(
            color: badgeColor.withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(isCompact ? 6 : 8),
            border: Border.all(
              color: badgeColor.withValues(alpha: 0.4),
              width: 1,
            ),
          ),
          child: FittedBox(
            fit: BoxFit.scaleDown,
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  formattedDisplayId,
                  style: TextStyle(
                    color: badgeColor,
                    fontWeight: FontWeight.bold,
                    fontSize: isCompact ? 9.5 : 12,
                    letterSpacing: isCompact ? 0.2 : 0.5,
                  ),
                ),
                if (isCopyable && !isCompact) ...[
                  const SizedBox(width: 4),
                  Icon(
                    Icons.copy_rounded,
                    size: 12,
                    color: badgeColor.withValues(alpha: 0.7),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
