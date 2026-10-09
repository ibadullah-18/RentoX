import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../../listing_create/data/photo_source.dart';
import '../../listing_create/domain/photo.dart';
import '../domain/account_models.dart';
import 'account_controller.dart';
import 'profile_avatar.dart';

/// Edit name, about-me text and photo.
class EditProfilePage extends ConsumerWidget {
  const EditProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final account = ref.watch(accountProvider);

    return SubPage(
      title: l10n.profileEditTitle,
      child: account.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) =>
            ErrorView(error: e, onRetry: () => ref.invalidate(accountProvider)),
        data: (a) =>
            a == null ? const SizedBox.shrink() : _EditForm(account: a),
      ),
    );
  }
}

class _EditForm extends ConsumerStatefulWidget {
  const _EditForm({required this.account});

  final Account account;

  @override
  ConsumerState<_EditForm> createState() => _EditFormState();
}

class _EditFormState extends ConsumerState<_EditForm> {
  late final TextEditingController _name = TextEditingController(
    text: widget.account.fullName,
  );
  late final TextEditingController _bio = TextEditingController(
    text: widget.account.bio,
  );
  bool _saving = false;
  bool _photoBusy = false;
  bool _submitted = false;

  @override
  void dispose() {
    _name.dispose();
    _bio.dispose();
    super.dispose();
  }

  bool get _dirty =>
      _name.text.trim() != widget.account.fullName.trim() ||
      _bio.text.trim() != widget.account.bio.trim();

  String? _nameError(AppL10n l10n) {
    if (!_submitted && _name.text.isEmpty) return null;
    return validateName(_name.text) == null ? null : l10n.profileNameInvalid;
  }

  void _toast(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _save() async {
    final l10n = AppL10n.of(context);
    setState(() => _submitted = true);
    if (validateName(_name.text) != null || _saving) return;
    setState(() => _saving = true);
    try {
      await ref
          .read(accountProvider.notifier)
          .save(fullName: _name.text, bio: _bio.text);
      if (!mounted) return;
      FocusScope.of(context).unfocus();
      _toast(l10n.profileSaved);
    } catch (e) {
      if (mounted) _toast(errorMessage(context, e));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _choosePhoto() async {
    final l10n = AppL10n.of(context);
    final source = ref.read(photoSourceProvider);
    final hasPhoto = widget.account.photoUrl != null;

    final choice = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
      ),
      builder: (context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ListTile(
            leading: const Icon(AppIcons.addPhoto),
            title: Text(l10n.addPhotos),
            onTap: () => Navigator.pop(context, 'gallery'),
          ),
          if (source.canCapture)
            ListTile(
              leading: const Icon(AppIcons.camera),
              title: Text(l10n.takePhoto),
              onTap: () => Navigator.pop(context, 'camera'),
            ),
          if (hasPhoto)
            ListTile(
              leading: const Icon(AppIcons.trash, color: AppColors.danger),
              title: Text(
                l10n.removePhoto,
                style: const TextStyle(color: AppColors.danger),
              ),
              onTap: () => Navigator.pop(context, 'remove'),
            ),
          const SizedBox(height: AppSpacing.md),
        ],
      ),
    );
    if (choice == null || !mounted) return;

    setState(() => _photoBusy = true);
    try {
      final controller = ref.read(accountProvider.notifier);
      if (choice == 'remove') {
        await controller.removePhoto();
      } else {
        final raw = choice == 'camera'
            ? [?await source.capture()]
            : await source.pickMany(1);
        if (raw.isEmpty) return;
        final result = PickedPhoto.tryCreate(
          name: raw.first.name,
          bytes: raw.first.bytes,
        );
        if (result.photo == null) {
          if (mounted) {
            _toast(
              result.issue == PhotoIssue.tooLarge
                  ? l10n.profilePhotoTooLarge
                  : l10n.photoUnsupported(1),
            );
          }
          return;
        }
        final problem = await controller.setPhoto(result.photo!);
        if (problem != null && mounted) _toast(l10n.profilePhotoTooLarge);
      }
    } catch (e) {
      if (mounted) _toast(errorMessage(context, e));
    } finally {
      if (mounted) setState(() => _photoBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    // Follow the stored account so a new photo shows at once.
    final account = ref.watch(accountProvider).value ?? widget.account;

    return ListView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        SubPage.topInset(context) + AppSpacing.lg,
        AppSpacing.lg,
        MediaQuery.paddingOf(context).bottom + AppSpacing.xl,
      ),
      children: [
        Center(
          child: GestureDetector(
            onTap: _photoBusy ? null : _choosePhoto,
            child: Stack(
              alignment: Alignment.center,
              children: [
                ProfileAvatar(account: account, size: 104),
                if (_photoBusy)
                  Container(
                    width: 104,
                    height: 104,
                    decoration: const BoxDecoration(
                      shape: BoxShape.circle,
                      color: Color(0x66000000),
                    ),
                    child: const Center(
                      child: SizedBox(
                        width: 26,
                        height: 26,
                        child: CircularProgressIndicator(
                          strokeWidth: 2.4,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),
                Positioned(
                  right: 0,
                  bottom: 0,
                  child: Container(
                    width: 34,
                    height: 34,
                    decoration: BoxDecoration(
                      color: scheme.primary,
                      shape: BoxShape.circle,
                      border: Border.all(color: scheme.surface, width: 2),
                    ),
                    child: const Icon(
                      AppIcons.camera,
                      size: 18,
                      color: Colors.white,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        Center(
          child: TextButton(
            onPressed: _photoBusy ? null : _choosePhoto,
            child: Text(
              account.photoUrl == null
                  ? l10n.profilePhotoAdd
                  : l10n.profilePhotoChange,
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        _Label(l10n.profileName),
        TextField(
          controller: _name,
          enabled: !_saving,
          maxLength: AccountRules.nameMax,
          textCapitalization: TextCapitalization.words,
          textInputAction: TextInputAction.next,
          decoration: InputDecoration(
            hintText: l10n.profileNameHint,
            errorText: _nameError(l10n),
            counterText: '',
          ),
          onChanged: (_) => setState(() {}),
        ),
        const SizedBox(height: AppSpacing.lg),
        _Label(l10n.profileBio),
        TextField(
          controller: _bio,
          enabled: !_saving,
          minLines: 3,
          maxLines: 6,
          maxLength: AccountRules.bioMax,
          textCapitalization: TextCapitalization.sentences,
          decoration: InputDecoration(hintText: l10n.profileBioHint),
          onChanged: (_) => setState(() {}),
        ),
        const SizedBox(height: AppSpacing.lg),
        _Label(l10n.profilePhone),
        InputDecorator(
          decoration: const InputDecoration(),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  account.phoneNumber,
                  style: TextStyle(color: scheme.onSurfaceVariant),
                ),
              ),
              Icon(AppIcons.lock, size: 18, color: scheme.onSurfaceVariant),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.xl),
        FilledButton(
          onPressed: (_dirty && !_saving) ? _save : null,
          child: _saving
              ? const SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.4,
                    color: Colors.white,
                  ),
                )
              : Text(l10n.profileSave),
        ),
      ],
    );
  }
}

class _Label extends StatelessWidget {
  const _Label(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6, left: 2),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 13,
          fontWeight: FontWeight.w700,
          color: Theme.of(context).colorScheme.onSurfaceVariant,
        ),
      ),
    );
  }
}
