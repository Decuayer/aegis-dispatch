class TaskMediaModel {
  final String id;
  final String mediaUrl;
  final String mediaType;
  final String? thumbnailUrl;
  final DateTime createdAt;

  const TaskMediaModel({
    required this.id,
    required this.mediaUrl,
    required this.mediaType,
    this.thumbnailUrl,
    required this.createdAt,
  });

  factory TaskMediaModel.fromJson(Map<String, dynamic> json) {
    return TaskMediaModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      mediaUrl: (json['mediaUrl'] ?? json['MediaUrl'] ?? '').toString(),
      mediaType: (json['mediaType'] ?? json['MediaType'] ?? 'image').toString(),
      thumbnailUrl:
          json['thumbnailUrl'] as String? ?? json['ThumbnailUrl'] as String?,
      createdAt:
          json['createdAt'] != null
              ? DateTime.tryParse(json['createdAt'].toString()) ??
                  DateTime.now()
              : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'mediaUrl': mediaUrl,
      'mediaType': mediaType,
      'thumbnailUrl': thumbnailUrl,
      'createdAt': createdAt.toIso8601String(),
    };
  }
}

class IncidentReportModel {
  final String id;
  final String incidentId;
  final String teamId;
  final String teamName;
  final String reportedByUserId;
  final String reportedByFullName;
  final String content;
  final String? mediaUrl;
  final DateTime reportedAt;

  const IncidentReportModel({
    required this.id,
    required this.incidentId,
    required this.teamId,
    required this.teamName,
    required this.reportedByUserId,
    required this.reportedByFullName,
    required this.content,
    this.mediaUrl,
    required this.reportedAt,
  });

  factory IncidentReportModel.fromJson(Map<String, dynamic> json) {
    return IncidentReportModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      incidentId: (json['incidentId'] ?? json['IncidentId'] ?? '').toString(),
      teamId: (json['teamId'] ?? json['TeamId'] ?? '').toString(),
      teamName: (json['teamName'] ?? json['TeamName'] ?? '').toString(),
      reportedByUserId:
          (json['reportedByUserId'] ?? json['ReportedByUserId'] ?? '')
              .toString(),
      reportedByFullName:
          (json['reportedByFullName'] ?? json['ReportedByFullName'] ?? '')
              .toString(),
      content: (json['content'] ?? json['Content'] ?? '').toString(),
      mediaUrl: json['mediaUrl'] as String? ?? json['MediaUrl'] as String?,
      reportedAt:
          json['reportedAt'] != null
              ? DateTime.tryParse(json['reportedAt'].toString()) ??
                  DateTime.now()
              : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'incidentId': incidentId,
      'teamId': teamId,
      'teamName': teamName,
      'reportedByUserId': reportedByUserId,
      'reportedByFullName': reportedByFullName,
      'content': content,
      'mediaUrl': mediaUrl,
      'reportedAt': reportedAt.toIso8601String(),
    };
  }
}

class TeamTaskModel {
  final String id;
  final String reporterId;
  final String reporterFullName;
  final String reporterPhone;
  final String reporterDepartment;
  final String reporterEmail;
  final String? reporterSubRole;
  final String? reporterAvatarUrl;

  final String category;
  final String emergencyCode;
  final String? description;
  final List<TaskMediaModel> mediaAttachments;
  final String status;
  final double latitude;
  final double longitude;
  final DateTime createdAt;
  final DateTime? assignedAt;
  final DateTime? completedAt;

  final String? assignedTeamId;
  final String? assignedTeamName;
  final String? completionNotes;
  final List<IncidentReportModel> reports;

  const TeamTaskModel({
    required this.id,
    required this.reporterId,
    required this.reporterFullName,
    required this.reporterPhone,
    required this.reporterDepartment,
    required this.reporterEmail,
    this.reporterSubRole,
    this.reporterAvatarUrl,
    required this.category,
    required this.emergencyCode,
    this.description,
    required this.mediaAttachments,
    required this.status,
    required this.latitude,
    required this.longitude,
    required this.createdAt,
    this.assignedAt,
    this.completedAt,
    this.assignedTeamId,
    this.assignedTeamName,
    this.completionNotes,
    required this.reports,
  });

  factory TeamTaskModel.fromJson(Map<String, dynamic> json) {
    var rawMedia = json['mediaAttachments'] ?? json['MediaAttachments'] ?? [];
    List<TaskMediaModel> mediaList = [];
    if (rawMedia is List) {
      mediaList =
          rawMedia
              .map((m) => TaskMediaModel.fromJson(m as Map<String, dynamic>))
              .toList();
    }

    var rawReports = json['reports'] ?? json['Reports'] ?? [];
    List<IncidentReportModel> reportList = [];
    if (rawReports is List) {
      reportList =
          rawReports
              .map(
                (r) => IncidentReportModel.fromJson(r as Map<String, dynamic>),
              )
              .toList();
    }

    return TeamTaskModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      reporterId: (json['reporterId'] ?? json['ReporterId'] ?? '').toString(),
      reporterFullName:
          (json['reporterFullName'] ?? json['ReporterFullName'] ?? '')
              .toString(),
      reporterPhone:
          (json['reporterPhone'] ?? json['ReporterPhone'] ?? '').toString(),
      reporterDepartment:
          (json['reporterDepartment'] ?? json['ReporterDepartment'] ?? '')
              .toString(),
      reporterEmail:
          (json['reporterEmail'] ?? json['ReporterEmail'] ?? '').toString(),
      reporterSubRole:
          json['reporterSubRole'] as String? ??
          json['ReporterSubRole'] as String?,
      reporterAvatarUrl:
          json['reporterAvatarUrl'] as String? ??
          json['ReporterAvatarUrl'] as String?,
      category: (json['category'] ?? json['Category'] ?? '').toString(),
      emergencyCode:
          (json['emergencyCode'] ?? json['EmergencyCode'] ?? '').toString(),
      description:
          json['description'] as String? ?? json['Description'] as String?,
      mediaAttachments: mediaList,
      status: (json['status'] ?? json['Status'] ?? 'Assigned').toString(),
      latitude: double.tryParse(json['latitude']?.toString() ?? '0.0') ?? 0.0,
      longitude: double.tryParse(json['longitude']?.toString() ?? '0.0') ?? 0.0,
      createdAt:
          json['createdAt'] != null
              ? DateTime.tryParse(json['createdAt'].toString()) ??
                  DateTime.now()
              : DateTime.now(),
      assignedAt:
          json['assignedAt'] != null
              ? DateTime.tryParse(json['assignedAt'].toString())
              : null,
      completedAt:
          json['completedAt'] != null
              ? DateTime.tryParse(json['completedAt'].toString())
              : null,
      assignedTeamId:
          json['assignedTeamId'] as String? ??
          json['AssignedTeamId'] as String?,
      assignedTeamName:
          json['assignedTeamName'] as String? ??
          json['AssignedTeamName'] as String?,
      completionNotes:
          json['completionNotes'] as String? ??
          json['CompletionNotes'] as String?,
      reports: reportList,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'reporterId': reporterId,
      'reporterFullName': reporterFullName,
      'reporterPhone': reporterPhone,
      'reporterDepartment': reporterDepartment,
      'reporterEmail': reporterEmail,
      'reporterSubRole': reporterSubRole,
      'reporterAvatarUrl': reporterAvatarUrl,
      'category': category,
      'emergencyCode': emergencyCode,
      'description': description,
      'mediaAttachments': mediaAttachments.map((m) => m.toJson()).toList(),
      'status': status,
      'latitude': latitude,
      'longitude': longitude,
      'createdAt': createdAt.toIso8601String(),
      'assignedAt': assignedAt?.toIso8601String(),
      'completedAt': completedAt?.toIso8601String(),
      'assignedTeamId': assignedTeamId,
      'assignedTeamName': assignedTeamName,
      'completionNotes': completionNotes,
      'reports': reports.map((r) => r.toJson()).toList(),
    };
  }

  TeamTaskModel copyWith({
    String? id,
    String? reporterId,
    String? reporterFullName,
    String? reporterPhone,
    String? reporterDepartment,
    String? reporterEmail,
    String? reporterSubRole,
    String? reporterAvatarUrl,
    String? category,
    String? emergencyCode,
    String? description,
    List<TaskMediaModel>? mediaAttachments,
    String? status,
    double? latitude,
    double? longitude,
    DateTime? createdAt,
    DateTime? assignedAt,
    DateTime? completedAt,
    String? assignedTeamId,
    String? assignedTeamName,
    String? completionNotes,
    List<IncidentReportModel>? reports,
  }) {
    return TeamTaskModel(
      id: id ?? this.id,
      reporterId: reporterId ?? this.reporterId,
      reporterFullName: reporterFullName ?? this.reporterFullName,
      reporterPhone: reporterPhone ?? this.reporterPhone,
      reporterDepartment: reporterDepartment ?? this.reporterDepartment,
      reporterEmail: reporterEmail ?? this.reporterEmail,
      reporterSubRole: reporterSubRole ?? this.reporterSubRole,
      reporterAvatarUrl: reporterAvatarUrl ?? this.reporterAvatarUrl,
      category: category ?? this.category,
      emergencyCode: emergencyCode ?? this.emergencyCode,
      description: description ?? this.description,
      mediaAttachments: mediaAttachments ?? this.mediaAttachments,
      status: status ?? this.status,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      createdAt: createdAt ?? this.createdAt,
      assignedAt: assignedAt ?? this.assignedAt,
      completedAt: completedAt ?? this.completedAt,
      assignedTeamId: assignedTeamId ?? this.assignedTeamId,
      assignedTeamName: assignedTeamName ?? this.assignedTeamName,
      completionNotes: completionNotes ?? this.completionNotes,
      reports: reports ?? this.reports,
    );
  }
}
