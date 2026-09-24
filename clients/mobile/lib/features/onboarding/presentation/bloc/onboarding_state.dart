abstract class OnboardingState {
  const OnboardingState();
}

class OnboardingInitial extends OnboardingState {
  const OnboardingInitial();
}

class OnboardingLoading extends OnboardingState {
  const OnboardingLoading();
}

class OnboardingRequired extends OnboardingState {
  final bool isConsentChecked;
  final bool isSubmitting;
  final String? errorMessage;
  final bool isPermanentlyDenied;

  const OnboardingRequired({
    this.isConsentChecked = false,
    this.isSubmitting = false,
    this.errorMessage,
    this.isPermanentlyDenied = false,
  });

  OnboardingRequired copyWith({
    bool? isConsentChecked,
    bool? isSubmitting,
    String? errorMessage,
    bool? isPermanentlyDenied,
  }) {
    return OnboardingRequired(
      isConsentChecked: isConsentChecked ?? this.isConsentChecked,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      errorMessage: errorMessage,
      isPermanentlyDenied: isPermanentlyDenied ?? this.isPermanentlyDenied,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is OnboardingRequired &&
          runtimeType == other.runtimeType &&
          isConsentChecked == other.isConsentChecked &&
          isSubmitting == other.isSubmitting &&
          errorMessage == other.errorMessage &&
          isPermanentlyDenied == other.isPermanentlyDenied;

  @override
  int get hashCode => Object.hash(
    isConsentChecked,
    isSubmitting,
    errorMessage,
    isPermanentlyDenied,
  );
}

class OnboardingCompleted extends OnboardingState {
  const OnboardingCompleted();
}
