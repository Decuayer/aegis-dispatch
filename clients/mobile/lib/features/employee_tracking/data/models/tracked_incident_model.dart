class TrackedIncidentMediaModel {
  final String id;
  final String mediaUrl;
  final int mediaType; // 1: Image, 2: Video
  final DateTime createdAt;

  const TrackedIncidentMediaModel({
    required this.id,
    required this.mediaUrl,
    required this.mediaType,
    required this.createdAt,
  });

  factory TrackedIncidentMediaModel.fromJson(Map<String, dynamic> json) {
    return TrackedIncidentMediaModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      mediaUrl: (json['mediaUrl'] ?? json['MediaUrl'] ?? '').toString(),
      mediaType: (json['mediaType'] ?? json['MediaType'] as num?)?.toInt() ?? 1,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'mediaUrl': mediaUrl,
      'mediaType': mediaType,
      'createdAt': createdAt.toIso8601String(),
    };
  }
}

class TrackedIncidentModel {
  final String id;
  final String reporterId;
  final String reporterFullName;
  final String category;
  final String emergencyCode;
  final String? description;
  final String status; // Open, Assigned, Resolved, Canceled
  final double latitude;
  final double longitude;
  final DateTime createdAt;
  final DateTime? assignedAt;
  final DateTime? completedAt;
  final List<TrackedIncidentMediaModel> mediaAttachments;

  // Assigned team operational details
  final String? assignedTeamId;
  final String? assignedTeamName;
  final String? assignedTeamLeaderName;
  final String? assignedTeamLeaderPhone;
  final String? assignedTeamStatus; // Idle, Forwarded, OnScene, Busy
  final int? assignedTeamMemberCount;
  final double? teamLatitude;
  final double? teamLongitude;

  const TrackedIncidentModel({
    required this.id,
    required this.reporterId,
    required this.reporterFullName,
    required this.category,
    required this.emergencyCode,
    this.description,
    required this.status,
    required this.latitude,
    required this.longitude,
    required this.createdAt,
    this.assignedAt,
    this.completedAt,
    this.mediaAttachments = const [],
    this.assignedTeamId,
    this.assignedTeamName,
    this.assignedTeamLeaderName,
    this.assignedTeamLeaderPhone,
    this.assignedTeamStatus,
    this.assignedTeamMemberCount,
    this.teamLatitude,
    this.teamLongitude,
  });

  // Acceptance criteria: Editing is disabled once incident is resolved or canceled
  bool get isEditable =>
      status.toLowerCase() != 'resolved' && status.toLowerCase() != 'canceled';

  // Elapsed duration since dispatch or creation
  Duration get durationSinceDispatch {
    final referenceTime = assignedAt ?? createdAt;
    return DateTime.now().difference(referenceTime);
  }

  factory TrackedIncidentModel.fromJson(Map<String, dynamic> json) {
    return TrackedIncidentModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      reporterId: (json['reporterId'] ?? json['ReporterId'] ?? '').toString(),
      reporterFullName: (json['reporterFullName'] ?? json['ReporterFullName'] ?? '').toString(),
      category: (json['category'] ?? json['Category'] ?? '').toString(),
      emergencyCode: (json['emergencyCode'] ?? json['EmergencyCode'] ?? '').toString(),
      description: json['description'] as String? ?? json['Description'] as String?,
      status: (json['status'] ?? json['Status'] ?? 'Open').toString(),
      latitude: (json['latitude'] ?? json['Latitude'] as num?)?.toDouble() ?? 0.0,
      longitude: (json['longitude'] ?? json['Longitude'] as num?)?.toDouble() ?? 0.0,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      assignedAt: json['assignedAt'] != null
          ? DateTime.tryParse(json['assignedAt'].toString())
          : null,
      completedAt: json['completedAt'] != null
          ? DateTime.tryParse(json['completedAt'].toString())
          : null,
      mediaAttachments: json['mediaAttachments'] != null
          ? (json['mediaAttachments'] as List)
              .map((item) => TrackedIncidentMediaModel.fromJson(item as Map<String, dynamic>))
              .toList()
          : const [],
      assignedTeamId: json['assignedTeamId']?.toString() ?? json['AssignedTeamId']?.toString(),
      assignedTeamName: json['assignedTeamName'] as String? ?? json['AssignedTeamName'] as String?,
      assignedTeamLeaderName: json['assignedTeamLeaderName'] as String? ?? json['AssignedTeamLeaderName'] as String?,
      assignedTeamLeaderPhone: json['assignedTeamLeaderPhone'] as String? ?? json['AssignedTeamLeaderPhone'] as String?,
      assignedTeamStatus: json['assignedTeamStatus'] as String? ?? json['AssignedTeamStatus'] as String?,
      assignedTeamMemberCount: (json['assignedTeamMemberCount'] ?? json['AssignedTeamMemberCount'] as num?)?.toInt(),
      teamLatitude: (json['teamLatitude'] ?? json['currentLatitude'] as num?)?.toDouble(),
      teamLongitude: (json['teamLongitude'] ?? json['currentLongitude'] as num?)?.toDouble(),
    );
  }

  TrackedIncidentModel copyWith({
    String? id,
    String? reporterId,
    String? reporterFullName,
    String? category,
    String? emergencyCode,
    String? description,
    String? status,
    double? latitude,
    double? longitude,
    DateTime? createdAt,
    DateTime? assignedAt,
    DateTime? completedAt,
    List<TrackedIncidentMediaModel>? mediaAttachments,
    String? assignedTeamId,
    String? assignedTeamName,
    String? assignedTeamLeaderName,
    String? assignedTeamLeaderPhone,
    String? assignedTeamStatus,
    int? assignedTeamMemberCount,
    double? teamLatitude,
    double? teamLongitude,
  }) {
    return TrackedIncidentModel(
      id: id ?? this.id,
      reporterId: reporterId ?? this.reporterId,
      reporterFullName: reporterFullName ?? this.reporterFullName,
      category: category ?? this.category,
      emergencyCode: emergencyCode ?? this.emergencyCode,
      description: description ?? this.description,
      status: status ?? this.status,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      createdAt: createdAt ?? this.createdAt,
      assignedAt: assignedAt ?? this.assignedAt,
      completedAt: completedAt ?? this.completedAt,
      mediaAttachments: mediaAttachments ?? this.mediaAttachments,
      assignedTeamId: assignedTeamId ?? this.assignedTeamId,
      assignedTeamName: assignedTeamName ?? this.assignedTeamName,
      assignedTeamLeaderName: assignedTeamLeaderName ?? this.assignedTeamLeaderName,
      assignedTeamLeaderPhone: assignedTeamLeaderPhone ?? this.assignedTeamLeaderPhone,
      assignedTeamStatus: assignedTeamStatus ?? this.assignedTeamStatus,
      assignedTeamMemberCount: assignedTeamMemberCount ?? this.assignedTeamMemberCount,
      teamLatitude: teamLatitude ?? this.teamLatitude,
      teamLongitude: teamLongitude ?? this.teamLongitude,
    );
  }
}
