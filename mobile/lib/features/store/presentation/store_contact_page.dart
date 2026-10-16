import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/brand_icon.dart';
import '../../../shared/widgets/sub_page.dart';
import '../domain/store_models.dart';
import 'store_controller.dart';
import 'store_form_page.dart' show storeIssueText;
import 'store_page.dart' show weekdayNames;

/// The contact side of the store: phone, e-mail, address, WhatsApp, networks and the working hours.
/// It can be changed at any time; a live store stays live.
class StoreContactPage extends ConsumerWidget {
  const StoreContactPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final store = ref.watch(myStoreProvider);

    return SubPage(
      title: l10n.storeContactEdit,
      child: store.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) =>
            ErrorView(error: e, onRetry: () => ref.invalidate(myStoreProvider)),
        data: (s) => s == null
            ? const SizedBox.shrink()
            : _ContactForm(initial: s.toContactForm()),
      ),
    );
  }
}

class _DayRow {
  _DayRow({required this.open, required this.from, required this.to});

  bool open;
  TimeOfDay from;
  TimeOfDay to;

  int get fromMinute => from.hour * 60 + from.minute;
  int get toMinute => to.hour * 60 + to.minute;
}

class _ContactForm extends ConsumerStatefulWidget {
  const _ContactForm({required this.initial});

  final StoreContactForm initial;

  @override
  ConsumerState<_ContactForm> createState() => _ContactFormState();
}

class _ContactFormState extends ConsumerState<_ContactForm> {
  late final Map<String, TextEditingController> _c;
  late final List<_DayRow> _days;
  Map<String, StoreIssue> _errors = {};
  bool _saving = false;

  static TimeOfDay _t(int minute) =>
      TimeOfDay(hour: minute ~/ 60 % 24, minute: minute % 60);

  @override
  void initState() {
    super.initState();
    final f = widget.initial;
    _c = {
      'phone': TextEditingController(text: f.phone),
      'email': TextEditingController(text: f.email),
      'address': TextEditingController(text: f.address),
      'whatsApp': TextEditingController(text: f.whatsApp),
      'whatsAppMessage': TextEditingController(text: f.whatsAppMessage),
      'instagram': TextEditingController(text: f.instagram),
      'tiktok': TextEditingController(text: f.tiktok),
      'youtube': TextEditingController(text: f.youtube),
      'facebook': TextEditingController(text: f.facebook),
      'website': TextEditingController(text: f.website),
    };
    _days = [
      for (var d = 1; d <= 7; d++)
        () {
          final h = f.hours.where((x) => x.day == d).firstOrNull;
          return h == null
              ? _DayRow(
                  open: false,
                  from: const TimeOfDay(hour: 9, minute: 0),
                  to: const TimeOfDay(hour: 18, minute: 0),
                )
              : _DayRow(
                  open: true,
                  from: _t(h.openMinute),
                  to: _t(h.closeMinute),
                );
        }(),
    ];
  }

  @override
  void dispose() {
    for (final c in _c.values) {
      c.dispose();
    }
    super.dispose();
  }

  StoreContactForm _read() => StoreContactForm(
    phone: _c['phone']!.text,
    email: _c['email']!.text,
    address: _c['address']!.text,
    whatsApp: _c['whatsApp']!.text,
    whatsAppMessage: _c['whatsAppMessage']!.text,
    instagram: _c['instagram']!.text,
    tiktok: _c['tiktok']!.text,
    youtube: _c['youtube']!.text,
    facebook: _c['facebook']!.text,
    website: _c['website']!.text,
    hours: [
      for (var i = 0; i < 7; i++)
        if (_days[i].open)
          StoreHours(
            day: i + 1,
            openMinute: _days[i].fromMinute,
            closeMinute: _days[i].toMinute,
          ),
    ],
  );

  Future<void> _pick(_DayRow row, {required bool start}) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: start ? row.from : row.to,
    );
    if (picked == null) return;
    setState(() {
      if (start) {
        row.from = picked;
      } else {
        row.to = picked;
      }
    });
  }

  void _copyFirst() {
    final first = _days.where((d) => d.open).firstOrNull ?? _days.first;
    setState(() {
      for (final d in _days) {
        d.from = first.from;
        d.to = first.to;
      }
    });
  }

  Future<void> _save() async {
    if (_saving) return;
    final form = _read();
    final errors = form.validate();
    setState(() => _errors = errors);
    if (errors.isNotEmpty) return;

    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _saving = true);
    try {
      await ref.read(myStoreProvider.notifier).saveContact(form);
      if (mounted) {
        messenger
          ..hideCurrentSnackBar()
          ..showSnackBar(SnackBar(content: Text(l10n.storeContactSaved)));
      }
    } catch (e) {
      if (mounted) {
        messenger
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(
              content: Text(
                e is ApiException
                    ? errorMessage(context, e)
                    : l10n.actionFailed,
              ),
            ),
          );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Widget _field(
    String key,
    String label, {
    TextInputType? type,
    int maxLines = 1,
    int? maxLength,
    Widget? icon,
  }) {
    final l10n = AppL10n.of(context);
    final issue = _errors[key];
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: TextField(
        controller: _c[key],
        enabled: !_saving,
        keyboardType: type,
        maxLines: maxLines,
        maxLength: maxLength,
        decoration: InputDecoration(
          labelText: label,
          counterText: '',
          prefixIcon: icon == null
              ? null
              : Padding(padding: const EdgeInsets.all(12), child: icon),
          errorText: issue == null
              ? null
              : issue == StoreIssue.invalidNumber
              ? l10n.storeErrNumber
              : storeIssueText(l10n, issue),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final names = weekdayNames(l10n);

    return ListView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        SubPage.topInset(context) + AppSpacing.md,
        AppSpacing.lg,
        MediaQuery.paddingOf(context).bottom + AppSpacing.xxl,
      ),
      children: [
        Text(
          l10n.storeContactEditHint,
          style: TextStyle(color: scheme.onSurfaceVariant),
        ),
        const SizedBox(height: AppSpacing.lg),
        _field('phone', l10n.storePhone, type: TextInputType.phone),
        _field('email', l10n.storeEmail, type: TextInputType.emailAddress),
        _field('address', l10n.storeAddress),
        _field(
          'whatsApp',
          l10n.storeWhatsApp,
          type: TextInputType.phone,
          icon: const BrandIcon(Brand.whatsapp, size: 22),
        ),
        _field(
          'whatsAppMessage',
          l10n.storeWhatsAppMessage,
          maxLines: 2,
          maxLength: 300,
        ),
        Padding(
          padding: const EdgeInsets.only(bottom: AppSpacing.lg),
          child: Text(
            l10n.storeWhatsAppHint,
            style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13),
          ),
        ),
        _field(
          'instagram',
          l10n.storeInstagram,
          type: TextInputType.url,
          icon: const BrandIcon(Brand.instagram, size: 22),
        ),
        _field(
          'tiktok',
          l10n.storeTiktok,
          type: TextInputType.url,
          icon: BrandIcon(Brand.tiktok, size: 22, color: scheme.onSurface),
        ),
        _field(
          'youtube',
          l10n.storeYoutube,
          type: TextInputType.url,
          icon: const BrandIcon(Brand.youtube, size: 22),
        ),
        _field(
          'facebook',
          l10n.storeFacebook,
          type: TextInputType.url,
          icon: const BrandIcon(Brand.facebook, size: 22),
        ),
        _field('website', l10n.storeWebsite, type: TextInputType.url),
        const SizedBox(height: AppSpacing.md),
        Row(
          children: [
            Expanded(
              child: Text(
                l10n.storeHoursTitle,
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            TextButton(
              onPressed: _saving ? null : _copyFirst,
              child: Text(l10n.storeHoursCopyAll),
            ),
          ],
        ),
        if (_errors['hours'] != null)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: Text(
              l10n.storeErrHours,
              style: TextStyle(color: scheme.error),
            ),
          ),
        for (var i = 0; i < 7; i++)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: DecoratedBox(
              decoration: BoxDecoration(
                border: Border.all(color: scheme.outlineVariant),
                borderRadius: BorderRadius.circular(AppRadius.lg),
              ),
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.md,
                  vertical: 4,
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(
                        names[i],
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                    ),
                    if (_days[i].open) ...[
                      TextButton(
                        onPressed: _saving
                            ? null
                            : () => _pick(_days[i], start: true),
                        child: Text(_days[i].from.format(context)),
                      ),
                      const Text('–'),
                      TextButton(
                        onPressed: _saving
                            ? null
                            : () => _pick(_days[i], start: false),
                        child: Text(_days[i].to.format(context)),
                      ),
                    ] else
                      Text(
                        l10n.storeDayClosed,
                        style: TextStyle(color: scheme.onSurfaceVariant),
                      ),
                    Switch(
                      value: _days[i].open,
                      onChanged: _saving
                          ? null
                          : (v) => setState(() => _days[i].open = v),
                    ),
                  ],
                ),
              ),
            ),
          ),
        const SizedBox(height: AppSpacing.lg),
        FilledButton(
          onPressed: _saving ? null : _save,
          style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
          child: _saving
              ? const SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(strokeWidth: 2.4),
                )
              : Text(l10n.storeSave),
        ),
      ],
    );
  }
}
