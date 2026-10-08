import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/shared/widgets/otp_field.dart';

void main() {
  Future<TextEditingController> pump(
    WidgetTester tester, {
    ValueChanged<String>? onCompleted,
  }) async {
    final controller = TextEditingController();
    addTearDown(controller.dispose);
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Center(
            child: OtpField(controller: controller, onCompleted: onCompleted),
          ),
        ),
      ),
    );
    return controller;
  }

  testWidgets('renders typed digits in boxes', (tester) async {
    await pump(tester);

    await tester.enterText(find.byType(TextField), '123');
    await tester.pump();

    expect(find.text('1'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
  });

  testWidgets('keeps digits only and caps at the code length', (tester) async {
    final controller = await pump(tester);

    await tester.enterText(find.byType(TextField), 'a1b2c3d4e5f6g7');
    await tester.pump();

    expect(controller.text, '123456');
  });

  testWidgets('calls onCompleted once the last digit is entered', (
    tester,
  ) async {
    String? completed;
    await pump(tester, onCompleted: (v) => completed = v);

    await tester.enterText(find.byType(TextField), '12345');
    await tester.pump();
    expect(completed, isNull);

    await tester.enterText(find.byType(TextField), '123456');
    await tester.pump();
    expect(completed, '123456');
  });
}
