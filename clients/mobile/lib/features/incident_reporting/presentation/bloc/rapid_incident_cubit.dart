import 'dart:async';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../data/models/rapid_emergency_preset.dart';
import '../../services/rapid_dispatch_service.dart';
import 'rapid_incident_state.dart';

class RapidIncidentCubit extends Cubit<RapidIncidentState> {
  final RapidDispatchService _dispatchService;
  Timer? _countdownTimer;

  RapidIncidentCubit({
    required RapidDispatchService dispatchService,
  })  : _dispatchService = dispatchService,
        super(const RapidIncidentInitial());

  /// Triggers instant emergency incident submission with heavy haptic feedback.
  Future<void> triggerRapidIncident(RapidEmergencyPreset preset) async {
    if (state is RapidIncidentSubmitting) return;

    _stopCountdown();

    // Tactile confirmation for high-stress conditions
    await HapticFeedback.heavyImpact();

    emit(RapidIncidentSubmitting(preset.id));

    try {
      final incident = await _dispatchService.dispatchRapidIncident(preset);
      emit(RapidIncidentSuccess(
        incident: incident,
        preset: preset,
        isReversible: true,
        remainingSeconds: 5,
      ));
      _startCountdown();
    } catch (e) {
      final message = e.toString().replaceAll('Exception: ', '').trim();
      emit(RapidIncidentFailure(
        message.isNotEmpty ? message : 'Emergency dispatch failed. Please try again.',
      ));
    }
  }

  /// Cancels the recently created emergency incident within the 5-second reversal window.
  Future<void> cancelDispatchedIncident(String incidentId) async {
    _stopCountdown();

    try {
      await _dispatchService.cancelDispatchedIncident(incidentId);
      emit(RapidIncidentCanceled(incidentId));
      emit(const RapidIncidentInitial());
    } catch (e) {
      final message = e.toString().replaceAll('Exception: ', '').trim();
      emit(RapidIncidentFailure(
        message.isNotEmpty ? message : 'Failed to cancel incident.',
      ));
    }
  }

  void _startCountdown() {
    _countdownTimer?.cancel();
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (state is RapidIncidentSuccess) {
        final current = state as RapidIncidentSuccess;
        final nextSeconds = current.remainingSeconds - 1;

        if (nextSeconds <= 0) {
          timer.cancel();
          emit(current.copyWith(
            isReversible: false,
            remainingSeconds: 0,
          ));
        } else {
          emit(current.copyWith(
            remainingSeconds: nextSeconds,
          ));
        }
      } else {
        timer.cancel();
      }
    });
  }

  void _stopCountdown() {
    _countdownTimer?.cancel();
    _countdownTimer = null;
  }

  /// Resets the cubit state back to initial.
  void resetState() {
    _stopCountdown();
    emit(const RapidIncidentInitial());
  }

  @override
  Future<void> close() {
    _stopCountdown();
    return super.close();
  }
}
