import '../../../profile/data/models/user_model.dart';

class AuthResponseModel {
  final String accessToken;
  final DateTime expiresAt;
  final UserModel user;

  const AuthResponseModel({
    required this.accessToken,
    required this.expiresAt,
    required this.user,
  });

  factory AuthResponseModel.fromJson(Map<String, dynamic> json) {
    final token = json['accessToken'] ?? json['AccessToken'] ?? '';
    final expires = json['expiresAt'] ?? json['ExpiresAt'];
    final userMap = json['user'] ?? json['User'] ?? {};

    return AuthResponseModel(
      accessToken: token.toString(),
      expiresAt:
          expires != null
              ? DateTime.tryParse(expires.toString()) ??
                  DateTime.now().add(const Duration(days: 7))
              : DateTime.now().add(const Duration(days: 7)),
      user: UserModel.fromJson(userMap is Map<String, dynamic> ? userMap : {}),
    );
  }
}
