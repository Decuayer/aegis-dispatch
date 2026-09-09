import 'package:google_sign_in/google_sign_in.dart';

class GoogleAuthService {
  static final GoogleSignIn _googleSignIn = GoogleSignIn(
    scopes: ['email', 'profile'],
  );

  /// Initiates Google Sign-In and returns the JWT ID Token.
  /// Returns null if the user cancelled the prompt.
  static Future<String?> signInAndGetIdToken() async {
    try {
      // Sign out first to ensure account picker appears if user wants to switch
      await _googleSignIn.signOut();
      final GoogleSignInAccount? account = await _googleSignIn.signIn();
      if (account == null) {
        return null; // User cancelled
      }
      final GoogleSignInAuthentication auth = await account.authentication;
      final idToken = auth.idToken;
      if (idToken == null || idToken.isEmpty) {
        throw Exception('Google sunucusundan ID token alınamadı.');
      }
      return idToken;
    } catch (e) {
      rethrow;
    }
  }

  /// Disconnects/signs out from Google session.
  static Future<void> signOut() async {
    try {
      await _googleSignIn.signOut();
    } catch (_) {}
  }
}
