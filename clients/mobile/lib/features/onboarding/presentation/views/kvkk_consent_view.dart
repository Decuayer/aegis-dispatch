import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../bloc/onboarding_bloc.dart';
import '../bloc/onboarding_event.dart';
import '../bloc/onboarding_state.dart';
import 'widgets/kvkk_disclosure_box.dart';
import 'widgets/permission_rationale_dialog.dart';

class KvkkConsentView extends StatelessWidget {
  const KvkkConsentView({super.key});

  void _showRationaleDialog(BuildContext context, bool isPermanentlyDenied) {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (ctx) => PermissionRationaleDialog(
            isPermanentlyDenied: isPermanentlyDenied,
            onAction: () {
              if (isPermanentlyDenied) {
                context.read<OnboardingBloc>().add(
                  const OnboardingOpenSettingsRequested(),
                );
              } else {
                context.read<OnboardingBloc>().add(
                  const OnboardingPermissionsRequested(),
                );
              }
            },
          ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: BlocConsumer<OnboardingBloc, OnboardingState>(
          listener: (context, state) {
            if (state is OnboardingRequired && state.errorMessage != null) {
              if (state.isPermanentlyDenied) {
                _showRationaleDialog(context, true);
              } else {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(state.errorMessage!),
                    backgroundColor: AppColors.error,
                    behavior: SnackBarBehavior.floating,
                    action: SnackBarAction(
                      label: 'Retry',
                      textColor: Colors.white,
                      onPressed: () {
                        context.read<OnboardingBloc>().add(
                          const OnboardingPermissionsRequested(),
                        );
                      },
                    ),
                  ),
                );
              }
            }
          },
          builder: (context, state) {
            final isChecked =
                state is OnboardingRequired && state.isConsentChecked;
            final isSubmitting =
                state is OnboardingRequired && state.isSubmitting;

            return Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(
                  horizontal: 24,
                  vertical: 16,
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Brand Logo
                    Center(
                      child: Container(
                        width: 88,
                        height: 88,
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(22),
                          border: Border.all(color: AppColors.border),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.05),
                              blurRadius: 18,
                              offset: const Offset(0, 6),
                            ),
                          ],
                        ),
                        child: Image.asset(
                          'assets/images/socar_logo.png',
                          fit: BoxFit.contain,
                        ),
                      ),
                    ),
                    const SizedBox(height: 20),

                    // Header Titles
                    const Text(
                      'SOCAR Dispatch',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.w800,
                        color: AppColors.primaryDark,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: 6),
                    const Text(
                      'Emergency Safety & Compliance Protocol',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 13,
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 28),

                    // KVKK Disclosure Text Box
                    const KvkkDisclosureBox(),
                    const SizedBox(height: 20),

                    // Consent Agreement Checkbox Card
                    InkWell(
                      borderRadius: BorderRadius.circular(12),
                      onTap:
                          isSubmitting
                              ? null
                              : () {
                                context.read<OnboardingBloc>().add(
                                  KvkkConsentToggled(!isChecked),
                                );
                              },
                      child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 6),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            SizedBox(
                              width: 24,
                              height: 24,
                              child: Checkbox(
                                value: isChecked,
                                activeColor: AppColors.primary,
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                onChanged:
                                    isSubmitting
                                        ? null
                                        : (val) {
                                          context.read<OnboardingBloc>().add(
                                            KvkkConsentToggled(val ?? false),
                                          );
                                        },
                              ),
                            ),
                            const SizedBox(width: 12),
                            const Expanded(
                              child: Text(
                                'I have read, understood, and accept the KVKK Clarification Text and Emergency Data Processing Terms.',
                                style: TextStyle(
                                  fontSize: 12,
                                  height: 1.45,
                                  color: AppColors.textPrimary,
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Continue CTA Button
                    ElevatedButton(
                      onPressed:
                          (!isChecked || isSubmitting)
                              ? null
                              : () {
                                context.read<OnboardingBloc>().add(
                                  const OnboardingPermissionsRequested(),
                                );
                              },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        disabledBackgroundColor: AppColors.primary.withValues(
                          alpha: 0.35,
                        ),
                        minimumSize: const Size.fromHeight(52),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                        ),
                      ),
                      child:
                          isSubmitting
                              ? const SizedBox(
                                height: 22,
                                width: 22,
                                child: CircularProgressIndicator(
                                  color: Colors.white,
                                  strokeWidth: 2.2,
                                ),
                              )
                              : const Text(
                                'Continue & Grant Permissions',
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w600,
                                  color: Colors.white,
                                ),
                              ),
                    ),
                    const SizedBox(height: 16),

                    // Security Footer Note
                    const Text(
                      'Emergency Operations • Encrypted Telemetry Active',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 11,
                        color: AppColors.textMuted,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
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
