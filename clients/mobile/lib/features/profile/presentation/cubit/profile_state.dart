import '../../data/models/user_model.dart';

abstract class ProfileState {
  const ProfileState();
}

class ProfileInitial extends ProfileState {
  const ProfileInitial();
}

class ProfileLoading extends ProfileState {
  const ProfileLoading();
}

class ProfileLoaded extends ProfileState {
  final UserModel user;

  const ProfileLoaded(this.user);
}

class ProfileUpdating extends ProfileState {
  final UserModel user;
  final bool isAvatarUpdating;

  const ProfileUpdating(this.user, {this.isAvatarUpdating = false});
}

class ProfileUpdateSuccess extends ProfileState {
  final UserModel user;
  final String message;

  const ProfileUpdateSuccess(this.user, this.message);
}

class ProfileError extends ProfileState {
  final String message;
  final UserModel? cachedUser;

  const ProfileError(this.message, {this.cachedUser});
}
