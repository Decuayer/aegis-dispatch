import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../employee_tracking/presentation/bloc/employee_tracking_bloc.dart';
import '../../../employee_tracking/presentation/bloc/employee_tracking_event.dart';
import '../../../employee_tracking/presentation/bloc/employee_tracking_state.dart';
import '../../../employee_tracking/presentation/views/incident_tracking_detail_view.dart';
import '../../../employee_tracking/presentation/widgets/active_incident_card.dart';
import '../../../incident_reporting/presentation/bloc/rapid_incident_cubit.dart';
import '../../../incident_reporting/presentation/bloc/rapid_incident_state.dart';
import '../../../incident_reporting/presentation/views/incident_report_wizard_view.dart';
import '../../../profile/data/models/user_model.dart';
import '../../../profile/presentation/views/profile_view.dart';
import '../widgets/emergency_dispatch_toast.dart';
import '../widgets/rapid_emergency_grid.dart';

class EmployeeHomeView extends StatefulWidget {
  final UserModel user;

  const EmployeeHomeView({super.key, required this.user});

  @override
  State<EmployeeHomeView> createState() => _EmployeeHomeViewState();
}

class _EmployeeHomeViewState extends State<EmployeeHomeView> {
  @override
  void initState() {
    super.initState();
    context.read<EmployeeTrackingBloc>().add(LoadMyIncidents(widget.user.id.toString()));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Field Dashboard'),
        actions: [
          IconButton(
            icon: const Icon(Icons.account_circle_outlined, size: 28),
            tooltip: 'Profile',
            onPressed: () {
              Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (ctx) => ProfileView(currentUser: widget.user),
                ),
              );
            },
          ),
        ],
      ),
      body: SafeArea(
        child: BlocConsumer<RapidIncidentCubit, RapidIncidentState>(
          listener: (context, state) {
            if (state is RapidIncidentFailure) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(state.errorMessage),
                  backgroundColor: AppColors.error,
                  behavior: SnackBarBehavior.floating,
                ),
              );
            } else if (state is RapidIncidentCanceled) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Emergency alert canceled successfully.'),
                  backgroundColor: AppColors.textSecondary,
                  behavior: SnackBarBehavior.floating,
                  duration: Duration(seconds: 3),
                ),
              );
            }
          },
          builder: (context, state) {
            return RefreshIndicator(
              onRefresh: () async {
                context.read<EmployeeTrackingBloc>().add(const RefreshMyIncidents());
              },
              child: SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // User Welcome Card
                    Container(
                      padding: const EdgeInsets.all(18),
                      decoration: BoxDecoration(
                        color: AppColors.primary,
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Welcome, ${widget.user.firstName}',
                            style: const TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.bold,
                              color: Colors.white,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            'Department: ${widget.user.department}',
                            style: TextStyle(
                              fontSize: 14,
                              color: Colors.white.withValues(alpha: 0.8),
                            ),
                          ),
                        ],
                      ),
                    ),

                    // Active Emergency Dispatch Banner (Reversal Window)
                    if (state is RapidIncidentSuccess) ...[
                      const SizedBox(height: 16),
                      EmergencyDispatchToast(state: state),
                    ],

                    const SizedBox(height: 20),

                    // My Reported Incidents Section
                    const Row(
                      children: [
                        Icon(Icons.radar_rounded, color: AppColors.primary, size: 20),
                        SizedBox(width: 6),
                        Text(
                          'My Reported Incidents',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),

                    BlocBuilder<EmployeeTrackingBloc, EmployeeTrackingState>(
                      builder: (context, trackingState) {
                        if (trackingState is EmployeeTrackingLoading) {
                          return const Center(
                            child: Padding(
                              padding: EdgeInsets.symmetric(vertical: 20),
                              child: CircularProgressIndicator(),
                            ),
                          );
                        }

                        if (trackingState is EmployeeTrackingLoaded) {
                          if (trackingState.incidents.isEmpty) {
                            return Container(
                              padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 16),
                              decoration: BoxDecoration(
                                color: AppColors.surfaceMuted,
                                borderRadius: BorderRadius.circular(14),
                                border: Border.all(color: AppColors.border),
                              ),
                              child: const Row(
                                children: [
                                  Icon(Icons.check_circle_outline_rounded, color: AppColors.success),
                                  SizedBox(width: 10),
                                  Expanded(
                                    child: Text(
                                      'No active incidents reported. Facility status is normal.',
                                      style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
                                    ),
                                  ),
                                ],
                              ),
                            );
                          }

                          return ListView.builder(
                            shrinkWrap: true,
                            physics: const NeverScrollableScrollPhysics(),
                            itemCount: trackingState.incidents.length,
                            itemBuilder: (context, index) {
                              final incident = trackingState.incidents[index];
                              return ActiveIncidentCard(
                                incident: incident,
                                onTap: () {
                                  Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                      builder: (_) => BlocProvider.value(
                                        value: context.read<EmployeeTrackingBloc>(),
                                        child: IncidentTrackingDetailView(
                                          incidentId: incident.id,
                                        ),
                                      ),
                                    ),
                                  );
                                },
                              );
                            },
                          );
                        }

                        return const SizedBox.shrink();
                      },
                    ),

                    const SizedBox(height: 24),

                    // Rapid Action Section Header
                    const Row(
                      children: [
                        Icon(Icons.bolt_rounded, color: AppColors.primary, size: 20),
                        SizedBox(width: 6),
                        Text(
                          'Rapid Emergency Actions (1-Tap)',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),

                    // 2x2 Emergency Buttons Grid
                    const RapidEmergencyGrid(),

                    const SizedBox(height: 24),

                    // Standard Reporting Alternative
                    const Text(
                      'Standard Reporting',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Card(
                      elevation: 1,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14),
                      ),
                      child: ListTile(
                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
                        leading: Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: AppColors.accent.withValues(alpha: 0.1),
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: const Icon(Icons.description_outlined, color: AppColors.accent),
                        ),
                        title: const Text(
                          'Report Detailed Incident',
                          style: TextStyle(fontWeight: FontWeight.w600, fontSize: 15),
                        ),
                        subtitle: const Text(
                          'Step-by-step wizard with media attachments',
                          style: TextStyle(fontSize: 12),
                        ),
                        trailing: const Icon(Icons.arrow_forward_ios_rounded, size: 14),
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (ctx) => const IncidentReportWizardView(),
                            ),
                          );
                        },
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
