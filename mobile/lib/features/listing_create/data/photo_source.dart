import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

class RawPhoto {
  const RawPhoto({required this.name, required this.bytes});

  final String name;
  final Uint8List bytes;
}

/// Where photos come from. An interface so tests (and previews) can supply
/// photos without a real picker.
abstract class PhotoSource {
  /// Lets the user choose up to [limit] photos from the gallery.
  Future<List<RawPhoto>> pickMany(int limit);

  /// Takes one photo with the camera (not available on web).
  Future<RawPhoto?> capture();

  bool get canCapture;
}

class ImagePickerPhotoSource implements PhotoSource {
  final _picker = ImagePicker();

  // Phone photos are often 5-12 MB; the API accepts 10 MB. Downscaling and
  // re-encoding keeps uploads fast and well under the limit.
  static const _maxSide = 2048.0;
  static const _quality = 85;

  @override
  bool get canCapture => !kIsWeb;

  Future<RawPhoto> _read(XFile f) async =>
      RawPhoto(name: f.name, bytes: await f.readAsBytes());

  @override
  Future<List<RawPhoto>> pickMany(int limit) async {
    if (limit <= 0) return const [];
    final files = limit == 1
        ? [
            ?await _picker.pickImage(
              source: ImageSource.gallery,
              maxWidth: _maxSide,
              maxHeight: _maxSide,
              imageQuality: _quality,
            ),
          ]
        : await _picker.pickMultiImage(
            limit: limit,
            maxWidth: _maxSide,
            maxHeight: _maxSide,
            imageQuality: _quality,
          );
    return [for (final f in files.take(limit)) await _read(f)];
  }

  @override
  Future<RawPhoto?> capture() async {
    final f = await _picker.pickImage(
      source: ImageSource.camera,
      maxWidth: _maxSide,
      maxHeight: _maxSide,
      imageQuality: _quality,
    );
    return f == null ? null : _read(f);
  }
}

final photoSourceProvider = Provider<PhotoSource>(
  (ref) => ImagePickerPhotoSource(),
);
