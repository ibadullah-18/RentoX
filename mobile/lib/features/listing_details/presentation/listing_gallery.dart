import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/glass.dart';
import '../../catalog/domain/catalog_models.dart';

/// Swipeable photo carousel with a "1 / 5" counter. Tap opens the full-screen
/// viewer.
class ListingGallery extends StatefulWidget {
  const ListingGallery({super.key, required this.images, required this.height});

  final List<ListingImage> images;
  final double height;

  @override
  State<ListingGallery> createState() => _ListingGalleryState();
}

class _ListingGalleryState extends State<ListingGallery> {
  final _controller = PageController();
  int _index = 0;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _open() {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        fullscreenDialog: true,
        builder: (_) =>
            FullscreenGallery(images: widget.images, initialIndex: _index),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final images = widget.images;

    if (images.isEmpty) {
      return SizedBox(
        height: widget.height,
        child: const ColoredBox(
          color: AppColors.primarySoft,
          child: Center(
            child: Icon(AppIcons.image, size: 56, color: AppColors.primary),
          ),
        ),
      );
    }

    return SizedBox(
      height: widget.height,
      child: Stack(
        fit: StackFit.expand,
        children: [
          PageView.builder(
            controller: _controller,
            itemCount: images.length,
            onPageChanged: (i) => setState(() => _index = i),
            itemBuilder: (_, i) => GestureDetector(
              onTap: _open,
              child: _NetworkPhoto(url: images[i].url, fit: BoxFit.cover),
            ),
          ),
          if (images.length > 1)
            Positioned(
              right: 16,
              bottom: 40,
              child: GlassSurface(
                radius: AppRadius.md,
                blur: 14,
                shadow: false,
                tintOpacity: 0.55,
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 5,
                ),
                child: Text(
                  l10n.photoCounter(_index + 1, images.length),
                  style: const TextStyle(
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class FullscreenGallery extends StatefulWidget {
  const FullscreenGallery({
    super.key,
    required this.images,
    required this.initialIndex,
  });

  final List<ListingImage> images;
  final int initialIndex;

  @override
  State<FullscreenGallery> createState() => _FullscreenGalleryState();
}

class _FullscreenGalleryState extends State<FullscreenGallery> {
  late final PageController _controller = PageController(
    initialPage: widget.initialIndex,
  );
  late int _index = widget.initialIndex;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final total = widget.images.length;

    return Scaffold(
      backgroundColor: Colors.black,
      body: Stack(
        children: [
          PageView.builder(
            controller: _controller,
            itemCount: total,
            onPageChanged: (i) => setState(() => _index = i),
            itemBuilder: (_, i) => InteractiveViewer(
              minScale: 1,
              maxScale: 4,
              child: Center(
                child: _NetworkPhoto(
                  url: widget.images[i].url,
                  fit: BoxFit.contain,
                ),
              ),
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  GlassIconButton(
                    icon: AppIcons.close,
                    semanticLabel: l10n.backAction,
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                  const Spacer(),
                  if (total > 1)
                    Text(
                      l10n.photoCounter(_index + 1, total),
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _NetworkPhoto extends StatelessWidget {
  const _NetworkPhoto({required this.url, required this.fit});

  final String url;
  final BoxFit fit;

  @override
  Widget build(BuildContext context) {
    const placeholder = ColoredBox(
      color: AppColors.primarySoft,
      child: Center(
        child: Icon(AppIcons.image, size: 48, color: AppColors.primary),
      ),
    );
    return CachedNetworkImage(
      imageUrl: url,
      fit: fit,
      width: double.infinity,
      height: double.infinity,
      fadeInDuration: const Duration(milliseconds: 180),
      placeholder: (_, _) => placeholder,
      errorWidget: (_, _, _) => placeholder,
    );
  }
}
