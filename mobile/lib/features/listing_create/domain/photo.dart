import 'dart:typed_data';

/// Limits enforced by the backend (`Listing.MaximumImageCount`,
/// `ListingImageService`). Checking them here gives instant feedback instead
/// of failing halfway through an upload.
abstract final class PhotoRules {
  static const maxCount = 30;
  static const maxBytes = 10 * 1024 * 1024;
}

enum PhotoIssue { unsupportedType, tooLarge, empty }

/// A photo chosen by the user, held in memory (works the same on mobile and
/// web, and keeps retries simple).
class PickedPhoto {
  const PickedPhoto({
    required this.name,
    required this.bytes,
    required this.mimeType,
  });

  final String name;
  final Uint8List bytes;
  final String mimeType;

  int get sizeBytes => bytes.length;

  /// Builds a photo from raw bytes. Returns the problem instead when the file
  /// can't be accepted by the backend.
  static ({PickedPhoto? photo, PhotoIssue? issue}) tryCreate({
    required String name,
    required Uint8List bytes,
  }) {
    if (bytes.isEmpty) return (photo: null, issue: PhotoIssue.empty);
    if (bytes.length > PhotoRules.maxBytes) {
      return (photo: null, issue: PhotoIssue.tooLarge);
    }
    final mime = detectImageMime(bytes);
    if (mime == null) {
      return (photo: null, issue: PhotoIssue.unsupportedType);
    }
    return (
      photo: PickedPhoto(
        name: _withExtension(name, mime),
        bytes: bytes,
        mimeType: mime,
      ),
      issue: null,
    );
  }

  static String _withExtension(String name, String mime) {
    final ext = switch (mime) {
      'image/png' => 'png',
      'image/webp' => 'webp',
      _ => 'jpg',
    };
    final base = name.contains('.')
        ? name.substring(0, name.lastIndexOf('.'))
        : name;
    return '${base.isEmpty ? 'photo' : base}.$ext';
  }
}

/// Sniffs the real format from the file header (file names and `mimeType`
/// reported by pickers are not reliable). Only formats the backend accepts
/// are recognised; anything else returns `null`.
String? detectImageMime(Uint8List b) {
  if (b.length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) {
    return 'image/jpeg';
  }
  if (b.length >= 8 &&
      b[0] == 0x89 &&
      b[1] == 0x50 &&
      b[2] == 0x4E &&
      b[3] == 0x47 &&
      b[4] == 0x0D &&
      b[5] == 0x0A &&
      b[6] == 0x1A &&
      b[7] == 0x0A) {
    return 'image/png';
  }
  // WebP: "RIFF" .... "WEBP"
  if (b.length >= 12 &&
      b[0] == 0x52 &&
      b[1] == 0x49 &&
      b[2] == 0x46 &&
      b[3] == 0x46 &&
      b[8] == 0x57 &&
      b[9] == 0x45 &&
      b[10] == 0x42 &&
      b[11] == 0x50) {
    return 'image/webp';
  }
  return null;
}
