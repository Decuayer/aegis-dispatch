import 'package:flutter/material.dart';

enum FeedbackCategory { bugReport, improvement, operationalIssue }

extension FeedbackCategoryExtension on FeedbackCategory {
  String get label {
    switch (this) {
      case FeedbackCategory.bugReport:
        return 'Bug Report';
      case FeedbackCategory.improvement:
        return 'Improvement Suggestion';
      case FeedbackCategory.operationalIssue:
        return 'Operational Issue';
    }
  }

  String get tag {
    switch (this) {
      case FeedbackCategory.bugReport:
        return '[Bug Report]';
      case FeedbackCategory.improvement:
        return '[Improvement]';
      case FeedbackCategory.operationalIssue:
        return '[Operational Issue]';
    }
  }

  IconData get icon {
    switch (this) {
      case FeedbackCategory.bugReport:
        return Icons.bug_report_outlined;
      case FeedbackCategory.improvement:
        return Icons.lightbulb_outline;
      case FeedbackCategory.operationalIssue:
        return Icons.build_circle_outlined;
    }
  }
}
