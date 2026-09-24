import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../features/profile/data/models/user_model.dart';

class SecureStorageService {
  static const String _tokenKey = 'access_token';
  static const String _userKey = 'user_data';

  final FlutterSecureStorage _storage;

  SecureStorageService({FlutterSecureStorage? storage})
    : _storage =
          storage ??
          const FlutterSecureStorage(
            aOptions: AndroidOptions(encryptedSharedPreferences: true),
            iOptions: IOSOptions(
              accessibility: KeychainAccessibility.first_unlock,
            ),
          );

  Future<void> saveAccessToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
  }

  Future<String?> getAccessToken() async {
    return await _storage.read(key: _tokenKey);
  }

  Future<void> saveUserData(UserModel user) async {
    await _storage.write(key: _userKey, value: user.toRawJson());
  }

  Future<UserModel?> getUserData() async {
    final rawJson = await _storage.read(key: _userKey);
    if (rawJson == null || rawJson.isEmpty) {
      return null;
    }
    try {
      return UserModel.fromRawJson(rawJson);
    } catch (_) {
      return null;
    }
  }

  Future<void> clearSession() async {
    await _storage.delete(key: _tokenKey);
    await _storage.delete(key: _userKey);
  }
}
