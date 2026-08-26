class MediaUploadResponseModel {
  final String objectKey;
  final String mediaUrl;
  final int fileSizeBytes;
  final String contentType;

  const MediaUploadResponseModel({
    required this.objectKey,
    required this.mediaUrl,
    required this.fileSizeBytes,
    required this.contentType,
  });

  factory MediaUploadResponseModel.fromJson(Map<String, dynamic> json) {
    return MediaUploadResponseModel(
      objectKey: json['objectKey'] as String? ?? '',
      mediaUrl: json['mediaUrl'] as String? ?? '',
      fileSizeBytes: json['fileSizeBytes'] as int? ?? 0,
      contentType: json['contentType'] as String? ?? '',
    );
  }
}
