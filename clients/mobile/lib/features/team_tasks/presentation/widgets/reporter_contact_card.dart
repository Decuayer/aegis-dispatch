import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../../../core/constants/app_colors.dart';

class ReporterContactCard extends StatelessWidget {
  final String reporterName;
  final String reporterPhone;
  final String reporterDepartment;
  final String? reporterAvatarUrl;

  const ReporterContactCard({
    super.key,
    required this.reporterName,
    required this.reporterPhone,
    required this.reporterDepartment,
    this.reporterAvatarUrl,
  });

  Future<void> _makeCall(BuildContext context) async {
    final uri = Uri.parse('tel:$reporterPhone');
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri);
    }
  }

  Future<void> _sendSms(BuildContext context) async {
    final uri = Uri.parse('sms:$reporterPhone');
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        children: [
          CircleAvatar(
            radius: 22,
            backgroundColor: AppColors.primaryLight.withValues(alpha: 0.15),
            backgroundImage:
                reporterAvatarUrl != null ? NetworkImage(reporterAvatarUrl!) : null,
            child: reporterAvatarUrl == null
                ? const Icon(Icons.person, color: AppColors.primary)
                : null,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  reporterName,
                  style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                ),
                Text(
                  reporterDepartment,
                  style: const TextStyle(color: AppColors.textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          if (reporterPhone.isNotEmpty) ...[
            IconButton(
              icon: const Icon(Icons.phone, color: AppColors.secondary),
              tooltip: 'Call Reporter',
              onPressed: () => _makeCall(context),
            ),
            IconButton(
              icon: const Icon(Icons.message_outlined, color: AppColors.primary),
              tooltip: 'SMS Reporter',
              onPressed: () => _sendSms(context),
            ),
          ],
        ],
      ),
    );
  }
}
