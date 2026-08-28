import 'package:flutter/material.dart';
import '../../../../core/constants/app_colors.dart';

enum TeamStatus {
  idle,
  forwarded,
  enRoute,
  onScene,
  busy,
  resolved;

  static TeamStatus fromString(String? value) {
    switch (value?.toLowerCase()) {
      case 'forwarded':
      case 'assigned':
        return TeamStatus.forwarded;
      case 'enroute':
      case 'in_transit':
      case 'intransit':
        return TeamStatus.enRoute;
      case 'onscene':
      case 'on_scene':
        return TeamStatus.onScene;
      case 'busy':
        return TeamStatus.busy;
      case 'resolved':
        return TeamStatus.resolved;
      case 'idle':
      default:
        return TeamStatus.idle;
    }
  }

  // Backend TeamStatus enum values: Idle, Forwarded, OnScene, Busy
  String get apiValue {
    switch (this) {
      case TeamStatus.idle:
      case TeamStatus.resolved:
        return 'Idle';
      case TeamStatus.forwarded:
      case TeamStatus.enRoute:
        return 'Forwarded';
      case TeamStatus.onScene:
        return 'OnScene';
      case TeamStatus.busy:
        return 'Busy';
    }
  }

  String get label {
    switch (this) {
      case TeamStatus.idle:
        return 'Idle / Standby';
      case TeamStatus.forwarded:
        return 'Assigned';
      case TeamStatus.enRoute:
        return 'En Route';
      case TeamStatus.onScene:
        return 'On Scene';
      case TeamStatus.busy:
        return 'Busy';
      case TeamStatus.resolved:
        return 'Resolved';
    }
  }

  Color get color {
    switch (this) {
      case TeamStatus.idle:
        return AppColors.textMuted;
      case TeamStatus.forwarded:
        return AppColors.info;
      case TeamStatus.enRoute:
        return AppColors.warning;
      case TeamStatus.onScene:
        return AppColors.accent;
      case TeamStatus.busy:
        return AppColors.error;
      case TeamStatus.resolved:
        return AppColors.success;
    }
  }

  IconData get icon {
    switch (this) {
      case TeamStatus.idle:
        return Icons.pause_circle_outline;
      case TeamStatus.forwarded:
        return Icons.assignment_outlined;
      case TeamStatus.enRoute:
        return Icons.directions_run;
      case TeamStatus.onScene:
        return Icons.place_outlined;
      case TeamStatus.busy:
        return Icons.engineering_outlined;
      case TeamStatus.resolved:
        return Icons.check_circle_outline;
    }
  }
}

enum MemberStatus {
  available,
  enRoute,
  onScene,
  busy,
  offDuty;

  static MemberStatus fromString(String? value) {
    switch (value?.toLowerCase()) {
      case 'enroute':
      case 'in_transit':
        return MemberStatus.enRoute;
      case 'onscene':
        return MemberStatus.onScene;
      case 'busy':
        return MemberStatus.busy;
      case 'offduty':
      case 'off_duty':
        return MemberStatus.offDuty;
      case 'available':
      default:
        return MemberStatus.available;
    }
  }

  // Backend TeamMemberStatus enum values: Available, EnRoute, OnScene, Unavailable
  String get apiValue {
    switch (this) {
      case MemberStatus.available:
        return 'Available';
      case MemberStatus.enRoute:
        return 'EnRoute';
      case MemberStatus.onScene:
        return 'OnScene';
      case MemberStatus.busy:
      case MemberStatus.offDuty:
        return 'Unavailable';
    }
  }

  String get label {
    switch (this) {
      case MemberStatus.available:
        return 'Available';
      case MemberStatus.enRoute:
        return 'En Route';
      case MemberStatus.onScene:
        return 'On Scene';
      case MemberStatus.busy:
        return 'Busy';
      case MemberStatus.offDuty:
        return 'Off Duty';
    }
  }
}

class TeamMemberModel {
  final String userId;
  final String fullName;
  final String email;
  final String phone;
  final String department;
  final String? subRole;
  final MemberStatus memberStatus;
  final DateTime? statusUpdatedAt;
  final DateTime joinedAt;

  const TeamMemberModel({
    required this.userId,
    required this.fullName,
    required this.email,
    required this.phone,
    required this.department,
    this.subRole,
    required this.memberStatus,
    this.statusUpdatedAt,
    required this.joinedAt,
  });

  factory TeamMemberModel.fromJson(Map<String, dynamic> json) {
    return TeamMemberModel(
      userId: (json['userId'] ?? json['UserId'] ?? '').toString(),
      fullName: (json['fullName'] ?? json['FullName'] ?? '').toString(),
      email: (json['email'] ?? json['Email'] ?? '').toString(),
      phone: (json['phone'] ?? json['Phone'] ?? '').toString(),
      department: (json['department'] ?? json['Department'] ?? '').toString(),
      subRole: json['subRole'] as String? ?? json['SubRole'] as String?,
      memberStatus: MemberStatus.fromString(json['memberStatus'] ?? json['MemberStatus']),
      statusUpdatedAt: json['statusUpdatedAt'] != null
          ? DateTime.tryParse(json['statusUpdatedAt'].toString())
          : null,
      joinedAt: json['joinedAt'] != null
          ? DateTime.tryParse(json['joinedAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'userId': userId,
      'fullName': fullName,
      'email': email,
      'phone': phone,
      'department': department,
      'subRole': subRole,
      'memberStatus': memberStatus.apiValue,
      'statusUpdatedAt': statusUpdatedAt?.toIso8601String(),
      'joinedAt': joinedAt.toIso8601String(),
    };
  }
}

class TeamModel {
  final String id;
  final String teamName;
  final TeamStatus status;
  final String? leaderId;
  final String? leaderFullName;
  final double? currentLatitude;
  final double? currentLongitude;
  final DateTime updatedAt;
  final List<TeamMemberModel> members;

  const TeamModel({
    required this.id,
    required this.teamName,
    required this.status,
    this.leaderId,
    this.leaderFullName,
    this.currentLatitude,
    this.currentLongitude,
    required this.updatedAt,
    required this.members,
  });

  factory TeamModel.fromJson(Map<String, dynamic> json) {
    var rawMembers = json['members'] ?? json['Members'] ?? [];
    List<TeamMemberModel> memberList = [];
    if (rawMembers is List) {
      memberList = rawMembers
          .map((m) => TeamMemberModel.fromJson(m as Map<String, dynamic>))
          .toList();
    }

    return TeamModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      teamName: (json['teamName'] ?? json['TeamName'] ?? '').toString(),
      status: TeamStatus.fromString(json['status'] ?? json['Status']),
      leaderId: json['leaderId'] as String? ?? json['LeaderId'] as String?,
      leaderFullName: json['leaderFullName'] as String? ?? json['LeaderFullName'] as String?,
      currentLatitude: json['currentLatitude'] != null
          ? double.tryParse(json['currentLatitude'].toString())
          : null,
      currentLongitude: json['currentLongitude'] != null
          ? double.tryParse(json['currentLongitude'].toString())
          : null,
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      members: memberList,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'teamName': teamName,
      'status': status.apiValue,
      'leaderId': leaderId,
      'leaderFullName': leaderFullName,
      'currentLatitude': currentLatitude,
      'currentLongitude': currentLongitude,
      'updatedAt': updatedAt.toIso8601String(),
      'members': members.map((m) => m.toJson()).toList(),
    };
  }

  TeamModel copyWith({
    String? id,
    String? teamName,
    TeamStatus? status,
    String? leaderId,
    String? leaderFullName,
    double? currentLatitude,
    double? currentLongitude,
    DateTime? updatedAt,
    List<TeamMemberModel>? members,
  }) {
    return TeamModel(
      id: id ?? this.id,
      teamName: teamName ?? this.teamName,
      status: status ?? this.status,
      leaderId: leaderId ?? this.leaderId,
      leaderFullName: leaderFullName ?? this.leaderFullName,
      currentLatitude: currentLatitude ?? this.currentLatitude,
      currentLongitude: currentLongitude ?? this.currentLongitude,
      updatedAt: updatedAt ?? this.updatedAt,
      members: members ?? this.members,
    );
  }
}
