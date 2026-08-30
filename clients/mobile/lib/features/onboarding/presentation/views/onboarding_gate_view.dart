import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../splash/presentation/views/splash_view.dart';
import '../bloc/onboarding_bloc.dart';
import '../bloc/onboarding_state.dart';
import 'kvkk_consent_view.dart';

class OnboardingGateView extends StatelessWidget {
  final Widget child;

  const OnboardingGateView({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<OnboardingBloc, OnboardingState>(
      buildWhen: (previous, current) => previous != current,
      builder: (context, state) {
        if (state is OnboardingCompleted) {
          return child;
        } else if (state is OnboardingRequired) {
          return const KvkkConsentView();
        } else {
          return const SplashView();
        }
      },
    );
  }
}
