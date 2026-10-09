import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../domain/store_models.dart';
import 'store_controller.dart';

String storeIssueText(AppL10n l10n, StoreIssue issue) => switch (issue) {
  StoreIssue.required => l10n.fieldRequired,
  StoreIssue.tooShort => l10n.storeErrShort,
  StoreIssue.tooLong => l10n.storeErrLong,
  StoreIssue.invalidEmail => l10n.storeErrEmail,
  StoreIssue.invalidUrl => l10n.storeErrUrl,
};

/// Create the user's store, or edit it while it is a draft / rejected.
class StoreFormPage extends ConsumerWidget {
  const StoreFormPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final store = ref.watch(myStoreProvider);

    return SubPage(
      title: store.value == null ? l10n.storeCreateTitle : l10n.storeEditTitle,
      fallbackRoute: Routes.myStore,
      child: store.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) =>
            ErrorView(error: e, onRetry: () => ref.invalidate(myStoreProvider)),
        data: (s) => s != null && !s.status.canEdit
            ? Padding(
                padding: EdgeInsets.only(top: SubPage.topInset(context)),
                child: EmptyView(
                  icon: AppIcons.lock,
                  title: l10n.editNotAllowed,
                  message: l10n.storeEditOnlyDraft,
                ),
              )
            : _Form(existing: s),
      ),
    );
  }
}

class _Form extends ConsumerStatefulWidget {
  const _Form({required this.existing});

  final MyStore? existing;

  @override
  ConsumerState<_Form> createState() => _FormState();
}

class _FormState extends ConsumerState<_Form> {
  late final StoreForm _initial =
      widget.existing?.toForm() ?? const StoreForm();
  late final _c = {
    StoreField.name: TextEditingController(text: _initial.name),
    StoreField.description: TextEditingController(text: _initial.description),
    StoreField.phone: TextEditingController(text: _initial.phone),
    StoreField.email: TextEditingController(text: _initial.email),
    StoreField.address: TextEditingController(text: _initial.address),
    StoreField.instagram: TextEditingController(text: _initial.instagram),
    StoreField.tiktok: TextEditingController(text: _initial.tiktok),
    StoreField.facebook: TextEditingController(text: _initial.facebook),
    StoreField.website: TextEditingController(text: _initial.website),
  };

  bool _saving = false;
  bool _showErrors = false;

  @override
  void dispose() {
    for (final c in _c.values) {
      c.dispose();
    }
    super.dispose();
  }

  StoreForm get _form => StoreForm(
    name: _c[StoreField.name]!.text,
    description: _c[StoreField.description]!.text,
    phone: _c[StoreField.phone]!.text,
    email: _c[StoreField.email]!.text,
    address: _c[StoreField.address]!.text,
    instagram: _c[StoreField.instagram]!.text,
    tiktok: _c[StoreField.tiktok]!.text,
    facebook: _c[StoreField.facebook]!.text,
    website: _c[StoreField.website]!.text,
  );

  Future<void> _save() async {
    if (_saving) return;
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _showErrors = true);
    if (_form.validate().isNotEmpty) return;

    setState(() => _saving = true);
    final creating = widget.existing == null;
    try {
      final controller = ref.read(myStoreProvider.notifier);
      if (creating) {
        await controller.create(_form);
      } else {
        await controller.save(_form);
      }
      if (!mounted) return;
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(creating ? l10n.storeCreated : l10n.storeSaved),
          ),
        );
      Navigator.of(context).maybePop();
    } catch (e) {
      if (mounted) {
        messenger
          ..hideCurrentSnackBar()
          ..showSnackBar(SnackBar(content: Text(errorMessage(context, e))));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Widget _field(
    StoreField field,
    String label, {
    int? maxLength,
    int minLines = 1,
    int maxLines = 1,
    TextInputType? keyboard,
    TextCapitalization caps = TextCapitalization.none,
  }) {
    final l10n = AppL10n.of(context);
    final issue = _showErrors ? _form.validate()[field] : null;
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.lg),
      child: TextField(
        controller: _c[field],
        enabled: !_saving,
        maxLength: maxLength,
        minLines: minLines,
        maxLines: maxLines,
        keyboardType: keyboard,
        textCapitalization: caps,
        onChanged: (_) => setState(() {}),
        decoration: InputDecoration(
          labelText: label,
          alignLabelWithHint: maxLines > 1,
          counterText: maxLength != null && maxLines == 1 ? '' : null,
          errorText: issue == null ? null : storeIssueText(l10n, issue),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);

    return ListView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        SubPage.topInset(context) + AppSpacing.lg,
        AppSpacing.lg,
        MediaQuery.paddingOf(context).bottom + AppSpacing.xxl,
      ),
      children: [
        _field(
          StoreField.name,
          '${l10n.storeName} *',
          maxLength: StoreRules.nameMax,
          caps: TextCapitalization.words,
        ),
        _field(
          StoreField.description,
          '${l10n.storeDescription} *',
          maxLength: StoreRules.descriptionMax,
          minLines: 4,
          maxLines: 8,
          caps: TextCapitalization.sentences,
        ),
        _field(
          StoreField.phone,
          '${l10n.storePhone} *',
          maxLength: StoreRules.phoneMax,
          keyboard: TextInputType.phone,
        ),
        _field(
          StoreField.email,
          l10n.storeEmail,
          maxLength: StoreRules.emailMax,
          keyboard: TextInputType.emailAddress,
        ),
        _field(
          StoreField.address,
          l10n.storeAddress,
          maxLength: StoreRules.addressMax,
          minLines: 1,
          maxLines: 3,
        ),
        _field(
          StoreField.website,
          l10n.storeWebsite,
          keyboard: TextInputType.url,
        ),
        _field(
          StoreField.instagram,
          l10n.storeInstagram,
          keyboard: TextInputType.url,
        ),
        _field(
          StoreField.tiktok,
          l10n.storeTiktok,
          keyboard: TextInputType.url,
        ),
        _field(
          StoreField.facebook,
          l10n.storeFacebook,
          keyboard: TextInputType.url,
        ),
        const SizedBox(height: AppSpacing.md),
        FilledButton(
          onPressed: _saving ? null : _save,
          child: _saving
              ? const SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.4,
                    color: Colors.white,
                  ),
                )
              : Text(l10n.storeSave),
        ),
      ],
    );
  }
}
