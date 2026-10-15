import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/theme/app_spacing.dart';

/// Only places a reader can safely be sent to: web addresses, e-mail and phone.
Uri? safeLink(String url) {
  final uri = Uri.tryParse(url);
  if (uri == null) return null;
  const allowed = {'http', 'https', 'mailto', 'tel'};
  return allowed.contains(uri.scheme.toLowerCase()) ? uri : null;
}

enum _Kind { h2, h3, p, ul, ol }

class _Block {
  _Block(this.kind, [List<String>? lines]) : lines = lines ?? [];

  final _Kind kind;
  final List<String> lines;
}

/// Splits the page text into blocks: headings, paragraphs and lists.
List<_Block> _parseBlocks(String text) {
  final blocks = <_Block>[];
  for (final raw in text.replaceAll('\r\n', '\n').split('\n')) {
    final line = raw.trimRight();
    final last = blocks.isEmpty ? null : blocks.last;

    if (line.trim().isEmpty) {
      blocks.add(_Block(_Kind.p)); // a blank line ends the block before it
      continue;
    }
    final heading = RegExp(r'^(#{2,3})\s+(.*)$').firstMatch(line);
    if (heading != null) {
      blocks.add(
        _Block(heading.group(1)!.length == 2 ? _Kind.h2 : _Kind.h3, [
          heading.group(2)!,
        ]),
      );
      continue;
    }
    final bullet = RegExp(r'^[-*]\s+(.*)$').firstMatch(line);
    if (bullet != null) {
      if (last?.kind == _Kind.ul) {
        last!.lines.add(bullet.group(1)!);
      } else {
        blocks.add(_Block(_Kind.ul, [bullet.group(1)!]));
      }
      continue;
    }
    final numbered = RegExp(r'^\d+\.\s+(.*)$').firstMatch(line);
    if (numbered != null) {
      if (last?.kind == _Kind.ol) {
        last!.lines.add(numbered.group(1)!);
      } else {
        blocks.add(_Block(_Kind.ol, [numbered.group(1)!]));
      }
      continue;
    }
    if (last?.kind == _Kind.p) {
      last!.lines.add(line);
    } else {
      blocks.add(_Block(_Kind.p, [line]));
    }
  }
  return blocks.where((b) => b.kind != _Kind.p || b.lines.isNotEmpty).toList();
}

/// The page texts the staff write: ## headings, paragraphs, - lists, 1. lists,
/// **bold** and [links](address). Nothing is run or interpreted beyond that.
class MarkdownView extends StatelessWidget {
  const MarkdownView({super.key, required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final blocks = _parseBlocks(text);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final b in blocks)
          switch (b.kind) {
            _Kind.h2 => Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xl, bottom: 8),
              child: _RichLine(
                b.lines.first,
                style: theme.textTheme.titleLarge!.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            _Kind.h3 => Padding(
              padding: const EdgeInsets.only(top: AppSpacing.lg, bottom: 6),
              child: _RichLine(
                b.lines.first,
                style: theme.textTheme.titleMedium!.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            _Kind.p => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: _RichLine(
                b.lines.join('\n'),
                style: theme.textTheme.bodyMedium!.copyWith(height: 1.5),
              ),
            ),
            _Kind.ul || _Kind.ol => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (var i = 0; i < b.lines.length; i++)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 6),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SizedBox(
                            width: 24,
                            child: Text(
                              b.kind == _Kind.ul ? '•' : '${i + 1}.',
                              style: theme.textTheme.bodyMedium!.copyWith(
                                height: 1.5,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                          Expanded(
                            child: _RichLine(
                              b.lines[i],
                              style: theme.textTheme.bodyMedium!.copyWith(
                                height: 1.5,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          },
      ],
    );
  }
}

/// One piece of text with **bold** and [links](address) inside it.
class _RichLine extends StatefulWidget {
  const _RichLine(this.text, {required this.style});

  final String text;
  final TextStyle style;

  @override
  State<_RichLine> createState() => _RichLineState();
}

class _RichLineState extends State<_RichLine> {
  final _recognizers = <TapGestureRecognizer>[];

  @override
  void dispose() {
    for (final r in _recognizers) {
      r.dispose();
    }
    super.dispose();
  }

  List<InlineSpan> _spans(BuildContext context) {
    for (final r in _recognizers) {
      r.dispose();
    }
    _recognizers.clear();

    final primary = Theme.of(context).colorScheme.primary;
    final spans = <InlineSpan>[];
    final pattern = RegExp(r'\*\*[^*]+\*\*|\[[^\]]+\]\([^)\s]+\)');
    var last = 0;
    for (final m in pattern.allMatches(widget.text)) {
      if (m.start > last) {
        spans.add(TextSpan(text: widget.text.substring(last, m.start)));
      }
      final token = m.group(0)!;
      if (token.startsWith('**')) {
        spans.add(
          TextSpan(
            text: token.substring(2, token.length - 2),
            style: const TextStyle(fontWeight: FontWeight.w800),
          ),
        );
      } else {
        final label = token.substring(1, token.indexOf(']'));
        final uri = safeLink(
          token.substring(token.indexOf('](') + 2, token.length - 1),
        );
        if (uri == null) {
          spans.add(TextSpan(text: label));
        } else {
          final tap = TapGestureRecognizer()
            ..onTap = () =>
                launchUrl(uri, mode: LaunchMode.externalApplication);
          _recognizers.add(tap);
          spans.add(
            TextSpan(
              text: label,
              recognizer: tap,
              style: TextStyle(
                color: primary,
                fontWeight: FontWeight.w700,
                decoration: TextDecoration.underline,
              ),
            ),
          );
        }
      }
      last = m.end;
    }
    if (last < widget.text.length) {
      spans.add(TextSpan(text: widget.text.substring(last)));
    }
    return spans;
  }

  @override
  Widget build(BuildContext context) {
    return Text.rich(TextSpan(children: _spans(context)), style: widget.style);
  }
}
