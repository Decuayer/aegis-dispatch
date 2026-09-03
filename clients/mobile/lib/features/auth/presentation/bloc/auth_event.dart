import '../../data/models/register_request_model.dart';
import '../../../profile/data/models/user_model.dart';

abstract class AuthEvent {
  const AuthEvent();
}

class AuthCheckRequested extends AuthEvent {
  const AuthCheckRequested();
}

class AuthLoginRequested extends AuthEvent {
  final String email;
  final String password;

  const AuthLoginRequested({
    required this.email,
    required this.password,
  });
}

class AuthRegisterRequested extends AuthEvent {
  final RegisterRequestModel request;

  const AuthRegisterRequested(this.request);
}

class AuthGoogleLoginRequested extends AuthEvent {
  final String idToken;

  const AuthGoogleLoginRequested({required this.idToken});
}

class AuthUserUpdated extends AuthEvent {
  final UserModel updatedUser;

  const AuthUserUpdated(this.updatedUser);
}

class AuthLogoutRequested extends AuthEvent {
  const AuthLogoutRequested();
}
