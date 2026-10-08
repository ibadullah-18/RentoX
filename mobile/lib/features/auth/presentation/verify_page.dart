import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/otp_field.dart';
import '../data/auth_repository.dart';
import '../domain/auth_models.dart';
import 'auth_controller.dart';

class VerifyPage extends ConsumerStatefulWidget {
  const VerifyPage({super.key, required this.args, this.redirectTo});

  final OtpFlowArgs args;
  final String? redirectTo;

  @override
  ConsumerState<VerifyPage> createState() => _VerifyPageState();
}

class _VerifyPageState extends ConsumerState<VerifyPage> {
  final _code = TextEditingController();
  final _name = TextEditingController();
  final _nameKey = GlobalKey<FormState>();

  late OtpChallenge _challenge = widget.args.challenge;
  Timer? _ticker;
  int _secondsLeft = 0;
  bool _loading = false;
  String? _codeError;

  @override
  void initState() {
    super.initState();
    _startCountdown();
  }

  @override
  void dispose() {
    _ticker?.cancel();
    _code.dispose();
    _name.dispose();
    super.dispose();
  }

  void _startCountdown() {
    _ticker?.cancel();
    void tick() {
      final left = _challenge.resendAvailableAt
          .difference(DateTime.now().toUtc())
          .inSeconds;
      setState(() => _secondsLeft = left < 0 ? 0 : left + 1);
      if (left < 0) _ticker?.cancel();
    }

    tick();
    _ticker = Timer.periodic(const Duration(seconds: 1), (_) => tick());
  }

  Future<void> _resend() async {
    final repo = ref.read(authRepositoryProvider);
    setState(() => _loading = true);
    try {
      final next = widget.args.isRegistration
          ? await repo.requestRegistrationOtp(widget.args.phone)
          : await repo.requestLoginOtp(widget.args.phone);
      _challenge = next;
      _code.clear();
      _startCountdown();
    } catch (e) {
      _toast(e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _submit(String code) async {
    if (code.length != 6 || _loading) return;
    final isRegistration = widget.args.isRegistration;
    if (isRegistration && !_nameKey.currentState!.validate()) return;

    final repo = ref.read(authRepositoryProvider);
    setState(() {
      _loading = true;
      _codeError = null;
    });
    try {
      final session = isRegistration
          ? await repo.completeRegistration(
              challengeId: _challenge.challengeId,
              code: code,
              fullName: _name.text.trim(),
              language: PreferredLanguage.fromCode(
                Localizations.localeOf(context).languageCode,
              ),
            )
          : await repo.completeLogin(
              challengeId: _challenge.challengeId,
              code: code,
            );
      ref.read(authControllerProvider.notifier).signedIn(session);
      if (!mounted) return;
      context.go(widget.redirectTo ?? '/');
    } catch (e) {
      if (!mounted) return;
      if (e is ApiException &&
          e.statusCode != null &&
          e.statusCode! < 500 &&
          !e.isNetwork) {
        setState(() => _codeError = AppL10n.of(context).otpWrong);
        _code.clear();
      } else {
        _toast(e);
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _toast(Object e) =>
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(errorMessage(context, e))));

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final isRegistration = widget.args.isRegistration;

    return Scaffold(
      appBar: AppBar(),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: SingleChildScrollView(
              padding: AppSpacing.screenPadding,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    isRegistration ? l10n.registerTitle : l10n.otpTitle,
                    style: theme.textTheme.headlineMedium,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    isRegistration
                        ? l10n.registerSubtitle
                        : l10n.otpSubtitle(widget.args.phone),
                    style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
                  ),
                  const SizedBox(height: AppSpacing.xl),
                  if (isRegistration) ...[
                    Form(
                      key: _nameKey,
                      child: TextFormField(
                        controller: _name,
                        textCapitalization: TextCapitalization.words,
                        autofillHints: const [AutofillHints.name],
                        decoration: InputDecoration(
                          labelText: l10n.fullNameLabel,
                          hintText: l10n.fullNameHint,
                        ),
                        validator: (v) => (v ?? '').trim().length < 2
                            ? l10n.fullNameRequired
                            : null,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    Text(
                      l10n.otpSubtitle(widget.args.phone),
                      style: TextStyle(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),
                  ],
                  Center(
                    child: OtpField(
                      controller: _code,
                      autofocus: !isRegistration,
                      enabled: !_loading,
                      hasError: _codeError != null,
                      onChanged: (_) {
                        if (_codeError != null) {
                          setState(() => _codeError = null);
                        }
                      },
                      onCompleted: _submit,
                    ),
                  ),
                  if (_codeError != null) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Center(
                      child: Text(
                        _codeError!,
                        style: const TextStyle(
                          color: AppColors.danger,
                          fontSize: 13,
                        ),
                      ),
                    ),
                  ],
                  const SizedBox(height: AppSpacing.xl),
                  FilledButton(
                    onPressed: _loading ? null : () => _submit(_code.text),
                    child: _loading
                        ? const SizedBox(
                            height: 22,
                            width: 22,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.4,
                              color: Colors.white,
                            ),
                          )
                        : Text(
                            isRegistration
                                ? l10n.registerAction
                                : l10n.continueAction,
                          ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Center(
                    child: TextButton(
                      onPressed: (_secondsLeft > 0 || _loading)
                          ? null
                          : _resend,
                      child: Text(
                        _secondsLeft > 0
                            ? l10n.otpResendIn(_secondsLeft)
                            : l10n.otpResend,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
