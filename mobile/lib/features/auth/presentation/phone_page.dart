import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/rentox_logo.dart';
import '../data/auth_repository.dart';
import '../domain/auth_models.dart';

class PhonePage extends ConsumerStatefulWidget {
  const PhonePage({super.key, this.redirectTo});

  final String? redirectTo;

  @override
  ConsumerState<PhonePage> createState() => _PhonePageState();
}

class _PhonePageState extends ConsumerState<PhonePage> {
  final _controller = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _loading = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final repo = ref.read(authRepositoryProvider);
    final phone = AuthRepository.normalizePhone(_controller.text);

    setState(() => _loading = true);
    try {
      OtpFlowArgs args;
      try {
        final challenge = await repo.requestLoginOtp(phone);
        args = OtpFlowArgs(
          phone: phone,
          challenge: challenge,
          isRegistration: false,
        );
      } on AccountNotFoundException {
        final challenge = await repo.requestRegistrationOtp(phone);
        args = OtpFlowArgs(
          phone: phone,
          challenge: challenge,
          isRegistration: true,
        );
      }
      if (!mounted) return;
      context.push(
        Uri(
          path: '/auth/verify',
          queryParameters: {
            if (widget.redirectTo != null) 'from': widget.redirectTo,
          },
        ).toString(),
        extra: args,
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(errorMessage(context, e))));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        leading: context.canPop() ? const BackButton() : null,
        actions: [
          TextButton(
            onPressed: () => context.go('/'),
            child: Text(l10n.navHome),
          ),
        ],
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: SingleChildScrollView(
              padding: AppSpacing.screenPadding,
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SizedBox(height: AppSpacing.lg),
                    const RentoXMark(size: 72),
                    const SizedBox(height: AppSpacing.sm),
                    const RentoXLogo(size: 40),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      l10n.tagline,
                      style: TextStyle(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xxl),
                    Text(
                      l10n.loginTitle,
                      style: theme.textTheme.headlineMedium,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      l10n.loginSubtitle,
                      style: TextStyle(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xl),
                    TextFormField(
                      controller: _controller,
                      keyboardType: TextInputType.phone,
                      autofillHints: const [
                        AutofillHints.telephoneNumberNational,
                      ],
                      textInputAction: TextInputAction.done,
                      onFieldSubmitted: (_) => _submit(),
                      inputFormatters: [
                        FilteringTextInputFormatter.digitsOnly,
                        LengthLimitingTextInputFormatter(12),
                      ],
                      decoration: InputDecoration(
                        labelText: l10n.phoneLabel,
                        hintText: l10n.phoneHint,
                        prefixIcon: const Padding(
                          padding: EdgeInsets.only(left: 16, right: 8),
                          child: Center(
                            widthFactor: 1,
                            child: Text(
                              '+994',
                              style: TextStyle(fontWeight: FontWeight.w700),
                            ),
                          ),
                        ),
                      ),
                      validator: (v) => AuthRepository.isValidPhone(v ?? '')
                          ? null
                          : l10n.phoneInvalid,
                    ),
                    const SizedBox(height: AppSpacing.xl),
                    FilledButton(
                      onPressed: _loading ? null : _submit,
                      child: _loading
                          ? const SizedBox(
                              height: 22,
                              width: 22,
                              child: CircularProgressIndicator(
                                strokeWidth: 2.4,
                                color: Colors.white,
                              ),
                            )
                          : Text(l10n.continueAction),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
