class EmergencyCodeModel {
  final String id;
  final String code;
  final String colorHex;
  final String description;
  final int severityLevel;
  final bool isActive;

  const EmergencyCodeModel({
    required this.id,
    required this.code,
    required this.colorHex,
    required this.description,
    required this.severityLevel,
    required this.isActive,
  });

  factory EmergencyCodeModel.fromJson(Map<String, dynamic> json) {
    return EmergencyCodeModel(
      id: json['id'] as String? ?? '',
      code: json['code'] as String? ?? '',
      colorHex: json['colorHex'] as String? ?? '#E30613',
      description: json['description'] as String? ?? '',
      severityLevel: json['severityLevel'] as int? ?? 1,
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'code': code,
      'colorHex': colorHex,
      'description': description,
      'severityLevel': severityLevel,
      'isActive': isActive,
    };
  }
}
