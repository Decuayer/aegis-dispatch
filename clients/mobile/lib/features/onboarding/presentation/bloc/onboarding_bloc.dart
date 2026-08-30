import 'package:flutter_bloc/flutter_bloc.dart';
import '../../data/onboarding_repository.dart';
import '../../services/onboarding_permission_service.dart';
import 'onboarding_event.dart';
import 'onboarding_state.dart';

class OnboardingBloc extends Bloc<OnboardingEvent, OnboardingState> {
  final OnboardingRepository _onboardingRepository;
  final OnboardingPermissionService _permissionService;

  OnboardingBloc({
    required OnboardingRepository onboardingRepository,
    required OnboardingPermissionService permissionService,
  })  : _onboardingRepository = onboardingRepository,
        _permissionService = permissionService,
        super(const OnboardingInitial()) {
    on<OnboardingCheckRequested>(_onCheckRequested);
    on<KvkkConsentToggled>(_onConsentToggled);
    on<OnboardingPermissionsRequested>(_onPermissionsRequested);
    on<OnboardingOpenSettingsRequested>(_onOpenSettingsRequested);
  }

  void _onCheckRequested(
    OnboardingCheckRequested event,
    Emitter<OnboardingState> emit,
  ) {
    emit(const OnboardingLoading());
    final hasAccepted = _onboardingRepository.hasAcceptedKvkk();
    if (hasAccepted) {
      emit(const OnboardingCompleted());
    } else {
      emit(const OnboardingRequired());
    }
  }

  void _onConsentToggled(
    KvkkConsentToggled event,
    Emitter<OnboardingState> emit,
  ) {
    if (state is OnboardingRequired) {
      final current = state as OnboardingRequired;
      emit(current.copyWith(
        isConsentChecked: event.isAccepted,
        errorMessage: null,
      ));
    }
  }

  Future<void> _onPermissionsRequested(
    OnboardingPermissionsRequested event,
    Emitter<OnboardingState> emit,
  ) async {
    if (state is! OnboardingRequired) return;
    final current = state as OnboardingRequired;
    if (!current.isConsentChecked) return;

    emit(current.copyWith(isSubmitting: true, errorMessage: null));

    final result = await _permissionService.requestOnboardingPermissions();

    switch (result) {
      case OnboardingPermissionResult.granted:
        await _onboardingRepository.setKvkkAccepted(true);
        emit(const OnboardingCompleted());
        break;
      case OnboardingPermissionResult.permanentlyDenied:
        emit(current.copyWith(
          isSubmitting: false,
          isPermanentlyDenied: true,
          errorMessage: 'Location permission is permanently denied. Please enable it in device settings.',
        ));
        break;
      case OnboardingPermissionResult.denied:
        emit(current.copyWith(
          isSubmitting: false,
          isPermanentlyDenied: false,
          errorMessage: 'Location permission is required for emergency dispatch operations.',
        ));
        break;
    }
  }

  Future<void> _onOpenSettingsRequested(
    OnboardingOpenSettingsRequested event,
    Emitter<OnboardingState> emit,
  ) async {
    await _permissionService.openSettings();
  }
}
