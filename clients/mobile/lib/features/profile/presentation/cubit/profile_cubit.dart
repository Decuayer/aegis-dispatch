import 'dart:io';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../data/models/update_profile_request_model.dart';
import '../../data/models/user_model.dart';
import '../../data/repositories/media_repository.dart';
import '../../data/repositories/profile_repository.dart';
import 'profile_state.dart';

class ProfileCubit extends Cubit<ProfileState> {
  final ProfileRepository _profileRepository;
  final MediaRepository _mediaRepository;

  ProfileCubit({
    required ProfileRepository profileRepository,
    required MediaRepository mediaRepository,
  })  : _profileRepository = profileRepository,
        _mediaRepository = mediaRepository,
        super(const ProfileInitial());

  Future<void> loadProfile({UserModel? initialUser}) async {
    if (initialUser != null) {
      emit(ProfileLoaded(initialUser));
    } else {
      emit(const ProfileLoading());
    }

    try {
      final user = await _profileRepository.fetchUserProfile();
      emit(ProfileLoaded(user));
    } catch (e) {
      final errorMsg = e.toString().replaceAll('Exception: ', '');
      emit(ProfileError(errorMsg, cachedUser: initialUser));
    }
  }

  Future<void> updateAvatar(File imageFile, UserModel currentUser) async {
    emit(ProfileUpdating(currentUser, isAvatarUpdating: true));
    try {
      final uploadedUrl = await _mediaRepository.uploadAvatar(imageFile);

      final updateRequest = UpdateProfileRequestModel(
        firstName: currentUser.firstName,
        lastName: currentUser.lastName,
        phone: currentUser.phone,
        department: currentUser.department,
        subRole: currentUser.subRole,
        avatarUrl: uploadedUrl,
      );

      final updatedUser = await _profileRepository.updateUserProfile(updateRequest);
      emit(ProfileUpdateSuccess(updatedUser, 'Avatar updated successfully.'));
    } catch (e) {
      final errorMsg = e.toString().replaceAll('Exception: ', '');
      emit(ProfileError(errorMsg, cachedUser: currentUser));
    }
  }

  Future<void> saveProfileDetails({
    required UserModel currentUser,
    required String firstName,
    required String lastName,
    required String phone,
    required String department,
    String? subRole,
  }) async {
    emit(ProfileUpdating(currentUser));
    try {
      final updateRequest = UpdateProfileRequestModel(
        firstName: firstName,
        lastName: lastName,
        phone: phone,
        department: department,
        subRole: subRole,
        avatarUrl: currentUser.avatarUrl,
      );

      final updatedUser = await _profileRepository.updateUserProfile(updateRequest);
      emit(ProfileUpdateSuccess(updatedUser, 'Profile updated successfully.'));
    } catch (e) {
      final errorMsg = e.toString().replaceAll('Exception: ', '');
      emit(ProfileError(errorMsg, cachedUser: currentUser));
    }
  }
}
