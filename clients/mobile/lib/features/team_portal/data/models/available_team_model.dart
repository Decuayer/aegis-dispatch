class AvailableTeamModel {
  final String id;
  final String teamName;
  final String? leaderId;
  final String? leaderFullName;
  final int memberCount;
  final DateTime createdAt;

  const AvailableTeamModel({
    required this.id,
    required this.teamName,
    this.leaderId,
    this.leaderFullName,
    required this.memberCount,
    required this.createdAt,
  });

  /// True if the team has an actively designated team leader.
  bool get hasLeader => leaderId != null && leaderId!.trim().isNotEmpty;

  factory AvailableTeamModel.fromJson(Map<String, dynamic> json) {
    return AvailableTeamModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      teamName: (json['teamName'] ?? json['TeamName'] ?? '').toString(),
      leaderId: json['leaderId'] as String? ?? json['LeaderId'] as String?,
      leaderFullName: json['leaderFullName'] as String? ?? json['LeaderFullName'] as String?,
      memberCount: int.tryParse((json['memberCount'] ?? json['MemberCount'] ?? 0).toString()) ?? 0,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : (json['CreatedAt'] != null
              ? DateTime.tryParse(json['CreatedAt'].toString()) ?? DateTime.now()
              : DateTime.now()),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'teamName': teamName,
      'leaderId': leaderId,
      'leaderFullName': leaderFullName,
      'memberCount': memberCount,
      'createdAt': createdAt.toIso8601String(),
    };
  }

  AvailableTeamModel copyWith({
    String? id,
    String? teamName,
    String? leaderId,
    String? leaderFullName,
    int? memberCount,
    DateTime? createdAt,
  }) {
    return AvailableTeamModel(
      id: id ?? this.id,
      teamName: teamName ?? this.teamName,
      leaderId: leaderId ?? this.leaderId,
      leaderFullName: leaderFullName ?? this.leaderFullName,
      memberCount: memberCount ?? this.memberCount,
      createdAt: createdAt ?? this.createdAt,
    );
  }
}
