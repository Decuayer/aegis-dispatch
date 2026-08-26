import '../../../profile/data/models/user_model.dart';

abstract class AuthState {
  const AuthState();
}

class AuthInitial extends AuthState {
  const AuthInitial();
}

class AuthLoading extends AuthState {
  const AuthLoading();
}

class Authenticated extends AuthState {
  final UserModel user;

  const Authenticated(this.user);
}

class Unauthenticated extends AuthState {
  const Unauthenticated();
}

class AuthFailure extends AuthState {
  final String errorMessage;

  const AuthFailure(this.errorMessage);
}
