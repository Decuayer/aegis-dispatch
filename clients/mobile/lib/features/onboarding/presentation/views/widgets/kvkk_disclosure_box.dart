import 'package:flutter/material.dart';
import '../../../../../core/constants/app_colors.dart';

class KvkkDisclosureBox extends StatefulWidget {
  const KvkkDisclosureBox({super.key});

  @override
  State<KvkkDisclosureBox> createState() => _KvkkDisclosureBoxState();
}

class _KvkkDisclosureBoxState extends State<KvkkDisclosureBox> {
  final ScrollController _scrollController = ScrollController();

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 220,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surfaceMuted,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Scrollbar(
        controller: _scrollController,
        thumbVisibility: true,
        radius: const Radius.circular(8),
        child: SingleChildScrollView(
          controller: _scrollController,
          padding: const EdgeInsets.only(right: 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: const [
              Text(
                'KVKK Clarification & Data Processing Disclosure',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
              SizedBox(height: 8),
              Text(
                'In accordance with Law No. 6698 on the Protection of Personal Data (KVKK), SOCAR Dispatch processes essential personal and telemetry data strictly for facility occupational health, emergency response, and life-safety operations across industrial plants.',
                style: TextStyle(
                  fontSize: 12,
                  height: 1.5,
                  color: AppColors.textSecondary,
                ),
              ),
              SizedBox(height: 10),
              Text(
                '1. Emergency Geolocation Tracking',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
              SizedBox(height: 4),
              Text(
                'Real-time GPS coordinates are collected during active incident missions and standby duty to calculate proximity, route response units, and coordinate safe evacuations in hazardous zones.',
                style: TextStyle(
                  fontSize: 12,
                  height: 1.5,
                  color: AppColors.textSecondary,
                ),
              ),
              SizedBox(height: 10),
              Text(
                '2. Incident Reporting & Media Evidence',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
              SizedBox(height: 4),
              Text(
                'Photographs, videos, and incident descriptions submitted by field personnel are securely encrypted and retained for post-incident investigation and HSE compliance.',
                style: TextStyle(
                  fontSize: 12,
                  height: 1.5,
                  color: AppColors.textSecondary,
                ),
              ),
              SizedBox(height: 10),
              Text(
                '3. Security & Access Control',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
              SizedBox(height: 4),
              Text(
                'All data transfers utilize TLS 1.3 encryption and access is strictly role-governed (Employees, Response Teams, Dispatch Operators) under ISO 27001 standards.',
                style: TextStyle(
                  fontSize: 12,
                  height: 1.5,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
