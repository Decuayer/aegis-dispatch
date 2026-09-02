import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../cubit/map_settings_cubit.dart';

class MapSettingsTile extends StatelessWidget {
  const MapSettingsTile({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<MapSettingsCubit, MapSettingsState>(
      builder: (context, state) {
        final isLocked = state.isBoundaryLockEnabled;

        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: AppColors.border),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Navigation & Map Settings',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 8),
              Material(
                color: Colors.transparent,
                child: SwitchListTile.adaptive(
                  contentPadding: EdgeInsets.zero,
                  secondary: Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: (isLocked ? AppColors.secondary : AppColors.textMuted)
                          .withValues(alpha: 0.12),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(
                      isLocked ? Icons.lock_outline_rounded : Icons.lock_open_rounded,
                      color: isLocked ? AppColors.secondary : AppColors.textMuted,
                      size: 22,
                    ),
                  ),
                  title: const Text(
                    'Lock Map to Facility Boundary',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  subtitle: const Text(
                    'Constrains map camera and viewport to SOCAR Aliaga & STAR Refinery grounds.',
                    style: TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  value: isLocked,
                  activeTrackColor: AppColors.secondary,
                  onChanged: (val) {
                    context.read<MapSettingsCubit>().toggleBoundaryLock(val);
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
