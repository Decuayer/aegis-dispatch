import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/widgets/entity_id_badge.dart';

class IncidentMapMarker extends StatelessWidget {
  final String? incidentId;

  const IncidentMapMarker({super.key, this.incidentId});

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (incidentId != null && incidentId!.isNotEmpty) ...[
          EntityIdBadge(
            id: incidentId!,
            type: EntityBadgeType.incident,
            isCompact: true,
            isCopyable: false,
          ),
          const SizedBox(height: 2),
        ],
        Container(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            color: AppColors.accent,
            shape: BoxShape.circle,
            border: Border.all(color: Colors.white, width: 2.5),
            boxShadow: const [
              BoxShadow(
                color: Colors.black38,
                blurRadius: 6,
                offset: Offset(0, 2),
              ),
            ],
          ),
          child: const Icon(
            Icons.local_fire_department,
            color: Colors.white,
            size: 22,
          ),
        ),
      ],
    );
  }
}

class ResponderMapMarker extends StatelessWidget {
  final String? teamId;

  const ResponderMapMarker({super.key, this.teamId});

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (teamId != null && teamId!.isNotEmpty) ...[
          EntityIdBadge(
            id: teamId!,
            type: EntityBadgeType.team,
            isCompact: true,
            isCopyable: false,
          ),
          const SizedBox(height: 2),
        ],
        Container(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            color: AppColors.primary,
            shape: BoxShape.circle,
            border: Border.all(color: Colors.white, width: 2.5),
            boxShadow: const [
              BoxShadow(
                color: Colors.black38,
                blurRadius: 6,
                offset: Offset(0, 2),
              ),
            ],
          ),
          child: const Icon(
            Icons.navigation_rounded,
            color: Colors.white,
            size: 20,
          ),
        ),
      ],
    );
  }
}
