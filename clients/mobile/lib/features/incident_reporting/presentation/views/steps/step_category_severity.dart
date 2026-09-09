import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../../core/constants/app_colors.dart';
import '../../bloc/incident_report_bloc.dart';
import '../../bloc/incident_report_event.dart';
import '../../bloc/incident_report_state.dart';

class StepCategorySeverity extends StatelessWidget {
  const StepCategorySeverity({super.key});

  static const List<Map<String, dynamic>> _categories = [
    {
      'name': 'Fire',
      'icon': Icons.local_fire_department_rounded,
      'color': Color(0xFFE53935),
    },
    {
      'name': 'Medical',
      'icon': Icons.medical_services_rounded,
      'color': Color(0xFF43A047),
    },
    {
      'name': 'Security',
      'icon': Icons.shield_rounded,
      'color': Color(0xFF1E88E5),
    },
    {
      'name': 'Environmental',
      'icon': Icons.eco_rounded,
      'color': Color(0xFF00ACC1),
    },
    {
      'name': 'Chemical',
      'icon': Icons.science_rounded,
      'color': Color(0xFFFB8C00),
    },
  ];

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<IncidentReportBloc, IncidentReportState>(
      builder: (context, state) {
        return SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                '1. Select Incident Category',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 12),
              GridView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 10,
                  mainAxisSpacing: 10,
                  childAspectRatio: 2.2,
                ),
                itemCount: _categories.length,
                itemBuilder: (context, index) {
                  final cat = _categories[index];
                  final isSelected = state.selectedCategory == cat['name'];
                  final color = cat['color'] as Color;

                  return InkWell(
                    onTap: () {
                      context.read<IncidentReportBloc>().add(
                        CategorySelected(cat['name'] as String),
                      );
                    },
                    borderRadius: BorderRadius.circular(12),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12),
                      decoration: BoxDecoration(
                        color:
                            isSelected
                                ? color.withValues(alpha: 0.12)
                                : AppColors.surface,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                          color: isSelected ? color : AppColors.border,
                          width: isSelected ? 2 : 1,
                        ),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            cat['icon'] as IconData,
                            color: isSelected ? color : AppColors.textSecondary,
                            size: 24,
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: Text(
                              cat['name'] as String,
                              style: TextStyle(
                                fontWeight:
                                    isSelected
                                        ? FontWeight.bold
                                        : FontWeight.w500,
                                color:
                                    isSelected ? color : AppColors.textPrimary,
                                fontSize: 13,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
              const SizedBox(height: 28),
              const Text(
                '2. Emergency Severity Code',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 12),
              if (state.isLoadingCodes)
                const Center(
                  child: Padding(
                    padding: EdgeInsets.all(20),
                    child: CircularProgressIndicator(),
                  ),
                )
              else if (state.emergencyCodes.isEmpty)
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppColors.surfaceMuted,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'No emergency codes found.',
                        style: TextStyle(color: AppColors.textSecondary),
                      ),
                      TextButton(
                        onPressed: () {
                          context.read<IncidentReportBloc>().add(
                            const LoadEmergencyCodesStarted(),
                          );
                        },
                        child: const Text('Retry'),
                      ),
                    ],
                  ),
                )
              else
                Column(
                  children:
                      state.emergencyCodes.map((code) {
                        final isSelected =
                            state.selectedEmergencyCode?.id == code.id;
                        Color badgeColor;
                        try {
                          final hex = code.colorHex.replaceFirst('#', '');
                          badgeColor = Color(int.parse('0xFF$hex'));
                        } catch (_) {
                          badgeColor = AppColors.accent;
                        }

                        return Padding(
                          padding: const EdgeInsets.only(bottom: 10),
                          child: InkWell(
                            onTap: () {
                              context.read<IncidentReportBloc>().add(
                                EmergencyCodeSelected(code),
                              );
                            },
                            borderRadius: BorderRadius.circular(12),
                            child: Container(
                              padding: const EdgeInsets.all(14),
                              decoration: BoxDecoration(
                                color:
                                    isSelected
                                        ? badgeColor.withValues(alpha: 0.1)
                                        : AppColors.surface,
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(
                                  color:
                                      isSelected
                                          ? badgeColor
                                          : AppColors.border,
                                  width: isSelected ? 2 : 1,
                                ),
                              ),
                              child: Row(
                                children: [
                                  Container(
                                    width: 14,
                                    height: 14,
                                    decoration: BoxDecoration(
                                      color: badgeColor,
                                      shape: BoxShape.circle,
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          'CODE ${code.code}',
                                          style: TextStyle(
                                            fontWeight: FontWeight.bold,
                                            fontSize: 14,
                                            color:
                                                isSelected
                                                    ? badgeColor
                                                    : AppColors.textPrimary,
                                          ),
                                        ),
                                        if (code.description.isNotEmpty)
                                          Text(
                                            code.description,
                                            style: const TextStyle(
                                              fontSize: 12,
                                              color: AppColors.textSecondary,
                                            ),
                                          ),
                                      ],
                                    ),
                                  ),
                                  if (isSelected)
                                    Icon(
                                      Icons.check_circle,
                                      color: badgeColor,
                                      size: 20,
                                    ),
                                ],
                              ),
                            ),
                          ),
                        );
                      }).toList(),
                ),
            ],
          ),
        );
      },
    );
  }
}
