class IncidentMediaModel {
  final String id;
  final String mediaUrl;
  final int mediaType;

  const IncidentMediaModel({
    required this.id,
    required this.mediaUrl,
    required this.mediaType,
  });

  factory IncidentMediaModel.fromJson(Map<String, dynamic> json) {
    return IncidentMediaModel(
      id: json['id'] as String? ?? '',
      mediaUrl: json['mediaUrl'] as String? ?? '',
      mediaType: json['mediaType'] as int? ?? 1,
    );
  }
}

class IncidentResponseModel {
  final String id;
  final String reporterFullName;
  final String category;
  final String emergencyCode;
  final String? description;
  final String status;
  final double latitude;
  final double longitude;
  final DateTime createdAt;
  final List<IncidentMediaModel> mediaAttachments;

  const IncidentResponseModel({
    required this.id,
    required this.reporterFullName,
    required this.category,
    required this.emergencyCode,
    this.description,
    required this.status,
    required this.latitude,
    required this.longitude,
    required this.createdAt,
    this.mediaAttachments = const [],
  });

  factory IncidentResponseModel.fromJson(Map<String, dynamic> json) {
    return IncidentResponseModel(
      id: json['id'] as String? ?? '',
      reporterFullName: json['reporterFullName'] as String? ?? '',
      category: json['category'] as String? ?? '',
      emergencyCode: json['emergencyCode'] as String? ?? '',
      description: json['description'] as String?,
      status: json['status'] as String? ?? 'Open',
      latitude: (json['latitude'] as num?)?.toDouble() ?? 0.0,
      longitude: (json['longitude'] as num?)?.toDouble() ?? 0.0,
      createdAt:
          json['createdAt'] != null
              ? DateTime.tryParse(json['createdAt'].toString()) ??
                  DateTime.now()
              : DateTime.now(),
      mediaAttachments:
          json['mediaAttachments'] != null
              ? (json['mediaAttachments'] as List)
                  .map(
                    (item) => IncidentMediaModel.fromJson(
                      item as Map<String, dynamic>,
                    ),
                  )
                  .toList()
              : const [],
    );
  }
}
