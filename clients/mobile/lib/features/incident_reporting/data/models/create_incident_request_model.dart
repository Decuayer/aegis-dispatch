enum IncidentMediaType {
  photo(1),
  video(2),
  audio(3);

  final int value;
  const IncidentMediaType(this.value);
}

class CreateIncidentMediaItem {
  final String mediaUrl;
  final IncidentMediaType mediaType;

  const CreateIncidentMediaItem({
    required this.mediaUrl,
    required this.mediaType,
  });

  Map<String, dynamic> toJson() {
    return {'mediaUrl': mediaUrl, 'mediaType': mediaType.value};
  }
}

class CreateIncidentRequestModel {
  final String category;
  final String emergencyCode;
  final String? description;
  final double latitude;
  final double longitude;
  final List<CreateIncidentMediaItem> mediaAttachments;

  const CreateIncidentRequestModel({
    required this.category,
    required this.emergencyCode,
    this.description,
    required this.latitude,
    required this.longitude,
    this.mediaAttachments = const [],
  });

  Map<String, dynamic> toJson() {
    return {
      'category': category,
      'emergencyCode': emergencyCode,
      'description': description,
      'latitude': latitude,
      'longitude': longitude,
      'mediaAttachments':
          mediaAttachments.map((item) => item.toJson()).toList(),
    };
  }
}
