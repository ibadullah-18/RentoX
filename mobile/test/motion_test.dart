import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/shared/widgets/motion.dart';

Widget host(Widget child) => MaterialApp(
  home: Scaffold(body: Center(child: child)),
);

void main() {
  testWidgets('Reveal ends fully visible and plays once per id', (t) async {
    await t.pumpWidget(
      host(const Reveal(id: 'a', index: 2, child: Text('hello'))),
    );
    await t.pump(const Duration(milliseconds: 60));
    await t.pumpAndSettle();
    expect(find.text('hello'), findsOneWidget);
    // Once finished the wrapper is gone: nothing left to fade or tilt.
    expect(
      find.ancestor(of: find.text('hello'), matching: find.byType(Opacity)),
      findsNothing,
    );

    // Same id again: shown immediately, no animation wrapper left waiting.
    await t.pumpWidget(
      host(const Reveal(id: 'a', index: 0, child: Text('again'))),
    );
    await t.pump();
    expect(find.text('again'), findsOneWidget);
  });

  testWidgets('far items in a list are not delayed', (t) async {
    await t.pumpWidget(
      host(const Reveal(id: 'far', index: 40, child: Text('far'))),
    );
    await t.pump();
    expect(find.text('far'), findsOneWidget);
  });

  testWidgets('PressScale shrinks while pressed and never blocks taps', (
    t,
  ) async {
    var taps = 0;
    await t.pumpWidget(
      host(
        PressScale(
          child: GestureDetector(
            onTap: () => taps++,
            child: const SizedBox(width: 80, height: 80, child: Text('btn')),
          ),
        ),
      ),
    );
    final gesture = await t.startGesture(t.getCenter(find.text('btn')));
    await t.pump(const Duration(milliseconds: 200));
    expect(
      t.widget<AnimatedScale>(find.byType(AnimatedScale)).scale,
      lessThan(1),
    );
    await gesture.up();
    await t.pumpAndSettle();
    expect(taps, 1);
    expect(t.widget<AnimatedScale>(find.byType(AnimatedScale)).scale, 1);
  });

  testWidgets('tab container shows only the active branch', (t) async {
    Widget build(int i) => host(
      FadeBranchContainer(
        currentIndex: i,
        children: const [Text('one'), Text('two')],
      ),
    );
    await t.pumpWidget(build(0));
    expect(find.text('one'), findsOneWidget);
    expect(find.text('two', skipOffstage: true), findsNothing);

    await t.pumpWidget(build(1));
    await t.pumpAndSettle();
    expect(find.text('two'), findsOneWidget);
    expect(find.text('one', skipOffstage: true), findsNothing);
    // The hidden branch stays mounted so its state survives.
    expect(find.text('one', skipOffstage: false), findsOneWidget);
  });
}
