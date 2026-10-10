/// What a report is about (the backend `ContentReportTarget`).
enum ReportTarget {
  listing('listings'),
  store('stores');

  const ReportTarget(this.path);

  /// The API path segment: `/api/{path}/{id}/reports`.
  final String path;
}

/// Why something is reported (the backend `ContentReportReason`).
enum ReportReason {
  spam(1),
  fraud(2),
  prohibited(3),
  misleading(4),
  wrongCategory(5),
  other(6);

  const ReportReason(this.id);
  final int id;

  /// "Other" needs an explanation; the rest may stand on their own.
  bool get needsDetails => this == other;

  /// Reasons that make sense for [target] (a store has no category).
  static List<ReportReason> forTarget(ReportTarget target) => [
    for (final r in values)
      if (!(target == ReportTarget.store && r == wrongCategory)) r,
  ];
}

abstract final class ReportRules {
  static const detailsMax = 1000;
}
