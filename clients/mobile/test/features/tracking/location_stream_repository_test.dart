import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/tracking/data/location_stream_repository.dart';

class MockEmptySecureStorage extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => null;
}

void main() {
  group('LocationStreamRepository Unit Tests', () {
    test(
      'startTracking returns false when user access token is null',
      () async {
        final repository = LocationStreamRepository(
          storageService: MockEmptySecureStorage(),
        );

        final result = await repository.startTracking('mock-team-123');
        expect(result, isFalse);
      },
    );
  });
}
