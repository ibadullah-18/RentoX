import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/features/auth/data/auth_repository.dart';

void main() {
  group('AuthRepository phone handling', () {
    test('normalises common Azerbaijani formats', () {
      expect(AuthRepository.normalizePhone('50 123 45 67'), '+994501234567');
      expect(AuthRepository.normalizePhone('050 123 45 67'), '+994501234567');
      expect(
        AuthRepository.normalizePhone('+994 50 123 45 67'),
        '+994501234567',
      );
    });

    test('validates length', () {
      expect(AuthRepository.isValidPhone('501234567'), isTrue);
      expect(AuthRepository.isValidPhone('5012345'), isFalse);
      expect(AuthRepository.isValidPhone(''), isFalse);
    });
  });
}
