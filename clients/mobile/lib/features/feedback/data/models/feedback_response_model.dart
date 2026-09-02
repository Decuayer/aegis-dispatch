class FeedbackMediaModel {
  final String id;
  final String feedbackId;
  final String mediaUrl;
  final String mediaType;
  final DateTime createdAt;

  const FeedbackMediaModel({
    required this.id,
    required this.feedbackId,
    required this.mediaUrl,
    required this.mediaType,
    required this.createdAt,
  });

  factory FeedbackMediaModel.fromJson(Map<String, dynamic> json) {
    return FeedbackMediaModel(
      id: json['id'] as String? ?? '',
      feedbackId: json['feedbackId'] as String? ?? '',
      mediaUrl: json['mediaUrl'] as String? ?? '',
      mediaType: json['mediaType'] as String? ?? '',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }
}

class FeedbackResponseModel {
  final String id;
  final String userId;
  final String userFullName;
  final String userEmail;
  final String title;
  final String description;
  final String status;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final List<FeedbackMediaModel> mediaAttachments;

  const FeedbackResponseModel({
    required this.id,
    required this.userId,
    required this.userFullName,
    required this.userEmail,
    required this.title,
    required this.description,
    required this.status,
    required this.createdAt,
    this.updatedAt,
    this.mediaAttachments = const [],
  });

  factory FeedbackResponseModel.fromJson(Map<String, dynamic> json) {
    return FeedbackResponseModel(
      id: json['id'] as String? ?? '',
      userId: json['userId'] as String? ?? '',
      userFullName: json['userFullName'] as String? ?? '',
      userEmail: json['userEmail'] as String? ?? '',
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      status: json['status']?.toString() ?? 'Pending',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'].toString())
          : null,
      mediaAttachments: json['mediaAttachments'] != null
          ? (json['mediaAttachments'] as List)
              .map((item) => FeedbackMediaModel.fromJson(item as Map<String, dynamic>))
              .toList()
          : const [],
    );
  }
}
