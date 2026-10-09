import 'package:flutter/material.dart';

/// Gentle "pressed" feedback: the child shrinks a little while a finger is
/// down. It only listens to raw pointers, so it never steals taps or scroll
/// gestures from the widget it wraps.
class PressScale extends StatefulWidget {
  const PressScale({super.key, required this.child, this.scale = 0.96});

  final Widget child;
  final double scale;

  @override
  State<PressScale> createState() => _PressScaleState();
}

class _PressScaleState extends State<PressScale> {
  bool _down = false;

  void _set(bool down) {
    if (_down != down && mounted) setState(() => _down = down);
  }

  @override
  Widget build(BuildContext context) {
    final animate = !MediaQuery.disableAnimationsOf(context);
    return Listener(
      onPointerDown: (_) => _set(true),
      onPointerUp: (_) => _set(false),
      onPointerCancel: (_) => _set(false),
      child: AnimatedScale(
        scale: _down && animate ? widget.scale : 1,
        duration: const Duration(milliseconds: 140),
        curve: Curves.easeOutCubic,
        child: widget.child,
      ),
    );
  }
}

/// Fades and lifts its child into place once, with a faint 3D tilt. Items
/// beyond [maxStagger] appear immediately so long lists never feel slow, and
/// the effect is skipped when the system asks for reduced motion.
class Reveal extends StatefulWidget {
  const Reveal({
    super.key,
    required this.child,
    required this.id,
    this.index = 0,
  });

  final Widget child;

  /// Identifies the item: each id is revealed once per app run, so scrolling
  /// away and back never replays the animation.
  final String id;
  final int index;

  static const maxStagger = 8;
  static final _seen = <String>{};

  @override
  State<Reveal> createState() => _RevealState();
}

class _RevealState extends State<Reveal> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 460),
  );
  late final Animation<double> _curve = CurvedAnimation(
    parent: _controller,
    curve: Curves.easeOutCubic,
  );
  bool _started = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_started) return;
    _started = true;
    if (MediaQuery.disableAnimationsOf(context) ||
        widget.index >= Reveal.maxStagger ||
        !Reveal._seen.add(widget.id)) {
      _controller.value = 1;
      return;
    }
    Future<void>.delayed(Duration(milliseconds: 55 * widget.index), () {
      if (mounted) _controller.forward();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _curve,
      child: widget.child,
      builder: (context, child) {
        final t = _curve.value;
        if (t >= 1) return child!;
        return Opacity(
          opacity: t,
          child: Transform(
            alignment: Alignment.bottomCenter,
            transform: Matrix4.identity()
              ..setEntry(3, 2, 0.001)
              ..translateByDouble(0, 18 * (1 - t), 0, 1)
              ..rotateX(0.18 * (1 - t)),
            child: child,
          ),
        );
      },
    );
  }
}

/// Cross-fades between the tab branches of the shell. Every branch stays
/// mounted (so scroll positions and state survive); only the selected one is
/// visible, interactive and ticking.
class FadeBranchContainer extends StatelessWidget {
  const FadeBranchContainer({
    super.key,
    required this.currentIndex,
    required this.children,
  });

  final int currentIndex;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Stack(
      fit: StackFit.expand,
      children: [
        for (var i = 0; i < children.length; i++)
          _FadeBranch(active: i == currentIndex, child: children[i]),
      ],
    );
  }
}

class _FadeBranch extends StatefulWidget {
  const _FadeBranch({required this.active, required this.child});

  final bool active;
  final Widget child;

  @override
  State<_FadeBranch> createState() => _FadeBranchState();
}

class _FadeBranchState extends State<_FadeBranch>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 240),
    value: widget.active ? 1 : 0,
  );

  @override
  void didUpdateWidget(_FadeBranch old) {
    super.didUpdateWidget(old);
    if (old.active == widget.active) return;
    if (MediaQuery.disableAnimationsOf(context)) {
      _controller.value = widget.active ? 1 : 0;
    } else {
      widget.active ? _controller.forward() : _controller.reverse();
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _controller,
      child: widget.child,
      builder: (context, child) {
        final t = Curves.easeOutCubic.transform(_controller.value);
        return Offstage(
          offstage: _controller.isDismissed,
          child: TickerMode(
            enabled: widget.active || !_controller.isDismissed,
            child: IgnorePointer(
              ignoring: !widget.active,
              child: Opacity(
                opacity: t,
                child: Transform.translate(
                  offset: Offset(0, 8 * (1 - t)),
                  child: child,
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}
