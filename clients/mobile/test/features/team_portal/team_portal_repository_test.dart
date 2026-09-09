import 'dart:convert';
import 'dart:typed_data';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/team_portal/data/repositories/team_portal_repository.dart';

class MockSecureStorageService extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'mock-jwt-token';
}

class MockHttpClientAdapter implements HttpClientAdapter {
  final Future<ResponseBody> Function(RequestOptions options) handler;

  MockHttpClientAdapter(this.handler);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) {
    return handler(options);
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  group('TeamPortalRepository Unit Tests', () {
    late ApiClient apiClient;
    late MockSecureStorageService storageService;

    setUp(() {
      storageService = MockSecureStorageService();
      apiClient = ApiClient(storageService: storageService);
    });

    test('getAvailableTeams parses list of open units correctly', () async {
      apiClient.dio.httpClientAdapter = MockHttpClientAdapter((options) async {
        expect(options.path, contains('/api/v1/teams/available'));
        expect(options.method, 'GET');

        final payload = {
          'success': true,
          'message': null,
          'data': [
            {
              'id': 'team-1',
              'teamName': 'Alpha Response Team',
              'leaderId': 'leader-1',
              'leaderFullName': 'Ali Veli',
              'memberCount': 3,
              'createdAt': '2026-08-31T01:00:00Z',
            },
            {
              'id': 'team-2',
              'teamName': 'Bravo Unit',
              'leaderId': null,
              'leaderFullName': null,
              'memberCount': 2,
              'createdAt': '2026-08-31T02:00:00Z',
            },
          ],
        };

        return ResponseBody.fromString(
          jsonEncode(payload),
          200,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );
      });

      final repository = TeamPortalRepository(apiClient: apiClient);
      final availableTeams = await repository.getAvailableTeams();

      expect(availableTeams.length, 2);
      expect(availableTeams[0].teamName, 'Alpha Response Team');
      expect(availableTeams[0].hasLeader, isTrue);
      expect(availableTeams[1].teamName, 'Bravo Unit');
      expect(availableTeams[1].hasLeader, isFalse);
    });

    test('joinTeam verifies path and sends correct userId payload', () async {
      apiClient.dio.httpClientAdapter = MockHttpClientAdapter((options) async {
        expect(options.path, contains('/api/v1/teams/team-1/members'));
        expect(options.method, 'POST');
        expect(options.data['userId'], 'user-100');

        final payload = {
          'success': true,
          'message': 'Joined successfully',
          'data': {
            'id': 'team-1',
            'teamName': 'Alpha Response Team',
            'status': 'Idle',
            'leaderId': 'leader-1',
            'members': [
              {
                'userId': 'user-100',
                'fullName': 'Test Responder',
                'email': 'responder@socar.az',
                'phone': '123456',
                'department': 'HSE',
                'memberStatus': 'Available',
              },
            ],
          },
        };

        return ResponseBody.fromString(
          jsonEncode(payload),
          200,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );
      });

      final repository = TeamPortalRepository(apiClient: apiClient);
      final team = await repository.joinTeam(
        teamId: 'team-1',
        userId: 'user-100',
      );

      expect(team.id, 'team-1');
      expect(team.members.length, 1);
      expect(team.members.first.userId, 'user-100');
    });

    test(
      'claimLeadership calls PUT with designated leaderId and teamName',
      () async {
        apiClient.dio.httpClientAdapter = MockHttpClientAdapter((
          options,
        ) async {
          expect(options.path, contains('/api/v1/teams/team-2'));
          expect(options.method, 'PUT');
          expect(options.data['teamName'], 'Bravo Unit');
          expect(options.data['leaderId'], 'user-100');

          final payload = {
            'success': true,
            'data': {
              'id': 'team-2',
              'teamName': 'Bravo Unit',
              'status': 'Idle',
              'leaderId': 'user-100',
              'members': [],
            },
          };

          return ResponseBody.fromString(
            jsonEncode(payload),
            200,
            headers: {
              Headers.contentTypeHeader: [Headers.jsonContentType],
            },
          );
        });

        final repository = TeamPortalRepository(apiClient: apiClient);
        final updated = await repository.claimLeadership(
          teamId: 'team-2',
          userId: 'user-100',
          teamName: 'Bravo Unit',
        );

        expect(updated.leaderId, 'user-100');
      },
    );

    test(
      'leaveTeam throws descriptive exception when blocked by active emergency response',
      () async {
        apiClient.dio.httpClientAdapter = MockHttpClientAdapter((
          options,
        ) async {
          expect(
            options.path,
            contains('/api/v1/teams/team-1/members/user-100'),
          );
          expect(options.method, 'DELETE');

          final payload = {
            'success': false,
            'message':
                'Cannot remove member while the team is involved in an active emergency response.',
          };

          return ResponseBody.fromString(
            jsonEncode(payload),
            400,
            headers: {
              Headers.contentTypeHeader: [Headers.jsonContentType],
            },
          );
        });

        final repository = TeamPortalRepository(apiClient: apiClient);

        expect(
          () => repository.leaveTeam(teamId: 'team-1', userId: 'user-100'),
          throwsA(
            predicate(
              (e) =>
                  e is Exception &&
                  e.toString().contains('active emergency response'),
            ),
          ),
        );
      },
    );
  });
}
