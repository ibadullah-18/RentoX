import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_spacing.dart';

/// One-time-code input drawn as separate boxes.
///
/// A real (invisible) [TextField] sits on top of the boxes, so the system
/// keyboard, paste, SMS autofill and accessibility all behave natively; the
/// boxes only render what the controller holds.
class OtpField extends StatefulWidget {
  const OtpField({
    super.key,
    required this.controller,
    this.length = 6,
    this.enabled = true,
    this.autofocus = false,
    this.hasError = false,
    this.onChanged,
    this.onCompleted,
  });

  final TextEditingController controller;
  final int length;
  final bool enabled;
  final bool autofocus;
  final bool hasError;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onCompleted;

  @override
  State<OtpField> createState() => _OtpFieldState();
}

class _OtpFieldState extends State<OtpField> {
  final _focus = FocusNode();

  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_rebuild);
    _focus.addListener(_rebuild);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_rebuild);
    _focus.dispose();
    super.dispose();
  }

  void _rebuild() {
    if (mounted) setState(() {});
  }

  void _handleChanged(String value) {
    widget.onChanged?.call(value);
    if (value.length == widget.length) widget.onCompleted?.call(value);
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final text = widget.controller.text;
    final activeIndex = text.length.clamp(0, widget.length - 1);
    const gap = 8.0;

    return LayoutBuilder(
      builder: (context, constraints) {
        final available = constraints.maxWidth.isFinite
            ? constraints.maxWidth
            : 340.0;
        final boxWidth =
            ((available - gap * (widget.length - 1)) / widget.length).clamp(
              36.0,
              52.0,
            );

        return Semantics(
          textField: true,
          label: 'OTP',
          child: Stack(
            alignment: Alignment.center,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  for (var i = 0; i < widget.length; i++) ...[
                    if (i > 0) const SizedBox(width: gap),
                    _Box(
                      width: boxWidth,
                      char: i < text.length ? text[i] : '',
                      focused: _focus.hasFocus && i == activeIndex,
                      error: widget.hasError,
                      scheme: scheme,
                    ),
                  ],
                ],
              ),
              // The real input: invisible, but covers the boxes so any tap
              // focuses it.
              Positioned.fill(
                child: Opacity(
                  opacity: 0,
                  child: TextField(
                    controller: widget.controller,
                    focusNode: _focus,
                    enabled: widget.enabled,
                    autofocus: widget.autofocus,
                    keyboardType: TextInputType.number,
                    textInputAction: TextInputAction.done,
                    autofillHints: const [AutofillHints.oneTimeCode],
                    enableInteractiveSelection: false,
                    showCursor: false,
                    maxLength: widget.length,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(
                      counterText: '',
                      border: InputBorder.none,
                      enabledBorder: InputBorder.none,
                      focusedBorder: InputBorder.none,
                      filled: false,
                    ),
                    onChanged: _handleChanged,
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _Box extends StatelessWidget {
  const _Box({
    required this.width,
    required this.char,
    required this.focused,
    required this.error,
    required this.scheme,
  });

  final double width;
  final String char;
  final bool focused;
  final bool error;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) {
    final borderColor = error
        ? AppColors.danger
        : focused
        ? scheme.primary
        : scheme.outline;

    return AnimatedContainer(
      duration: const Duration(milliseconds: 160),
      width: width,
      height: 58,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(AppRadius.md),
        border: Border.all(
          color: borderColor,
          width: focused || error ? 1.8 : 1,
        ),
      ),
      child: Text(
        char,
        style: Theme.of(context).textTheme.titleLarge
            ?.copyWith(fontWeight: FontWeight.w700),
      ),
    );
  }
}
