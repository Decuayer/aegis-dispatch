import 'package:flutter/material.dart';

class RapidEmergencyPreset {
  final String id;
  final String title;
  final String subtitle;
  final String category;
  final String emergencyCode;
  final IconData icon;
  final Color primaryColor;
  final Color backgroundColor;

  const RapidEmergencyPreset({
    required this.id,
    required this.title,
    required this.subtitle,
    required this.category,
    required this.emergencyCode,
    required this.icon,
    required this.primaryColor,
    required this.backgroundColor,
  });

  /// Predefined emergency presets compatible with backend categories and emergency codes.
  static const List<RapidEmergencyPreset> presets = [
    RapidEmergencyPreset(
      id: 'fire',
      title: 'Fire / Explosion',
      subtitle: 'Fire, flame, or explosion hazard',
      category: 'Fire',
      emergencyCode: 'Red',
      icon: Icons.local_fire_department_rounded,
      primaryColor: Color(0xFFD32F2F),
      backgroundColor: Color(0xFFFFEBEE),
    ),
    RapidEmergencyPreset(
      id: 'gas_leak',
      title: 'Gas Leak / Vapor',
      subtitle: 'Toxic gas leak or chemical hazard',
      category: 'Chemical',
      emergencyCode: 'Red',
      icon: Icons.warning_amber_rounded,
      primaryColor: Color(0xFFF57C00),
      backgroundColor: Color(0xFFFFF3E0),
    ),
    RapidEmergencyPreset(
      id: 'medical',
      title: 'Medical Emergency',
      subtitle: 'Injury, trauma, or loss of consciousness',
      category: 'Medical',
      emergencyCode: 'Yellow',
      icon: Icons.medical_services_rounded,
      primaryColor: Color(0xFF388E3C),
      backgroundColor: Color(0xFFE8F5E9),
    ),
    RapidEmergencyPreset(
      id: 'sos',
      title: 'SOS / Panic Alert',
      subtitle: 'Critical life-safety emergency',
      category: 'Security',
      emergencyCode: 'Red',
      icon: Icons.emergency_rounded,
      primaryColor: Color(0xFFB71C1C),
      backgroundColor: Color(0xFFFFCDD2),
    ),
  ];
}
