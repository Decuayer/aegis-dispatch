import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/storage/map_settings_repository.dart';

class MapSettingsState {
  final bool isBoundaryLockEnabled;

  const MapSettingsState({required this.isBoundaryLockEnabled});

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is MapSettingsState &&
          runtimeType == other.runtimeType &&
          isBoundaryLockEnabled == other.isBoundaryLockEnabled;

  @override
  int get hashCode => isBoundaryLockEnabled.hashCode;
}

class MapSettingsCubit extends Cubit<MapSettingsState> {
  final MapSettingsRepository _repository;

  MapSettingsCubit({required MapSettingsRepository repository})
      : _repository = repository,
        super(MapSettingsState(isBoundaryLockEnabled: repository.isBoundaryLockEnabled()));

  /// Updates local storage and emits the new boundary lock state
  Future<void> toggleBoundaryLock(bool enabled) async {
    await _repository.setBoundaryLockEnabled(enabled);
    emit(MapSettingsState(isBoundaryLockEnabled: enabled));
  }
}
