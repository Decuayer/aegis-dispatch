abstract class OnboardingEvent {
  const OnboardingEvent();
}

class OnboardingCheckRequested extends OnboardingEvent {
  const OnboardingCheckRequested();
}

class KvkkConsentToggled extends OnboardingEvent {
  final bool isAccepted;

  const KvkkConsentToggled(this.isAccepted);

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is KvkkConsentToggled &&
          runtimeType == other.runtimeType &&
          isAccepted == other.isAccepted;

  @override
  int get hashCode => isAccepted.hashCode;
}

class OnboardingPermissionsRequested extends OnboardingEvent {
  const OnboardingPermissionsRequested();
}

class OnboardingOpenSettingsRequested extends OnboardingEvent {
  const OnboardingOpenSettingsRequested();
}
