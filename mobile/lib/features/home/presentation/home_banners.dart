import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/theme/app_spacing.dart';
import '../../catalog/data/catalog_repository.dart';
import '../domain/home_banner.dart';

/// The banners at the top of the home page: one picture at a time, swiping, with dots when there are several.
class HomeBanners extends ConsumerStatefulWidget {
  const HomeBanners({super.key});

  @override
  ConsumerState<HomeBanners> createState() => _HomeBannersState();
}

class _HomeBannersState extends ConsumerState<HomeBanners>
    with SingleTickerProviderStateMixin {
  /// How long one banner stays before the next one slides in.
  static const _every = Duration(seconds: 5);

  // The strip is endless: the page number only grows, and the banner shown is that number modulo the count. So the
  // automatic move is always the same way and the last banner is followed by the first.
  static const _start = 1000;
  // One banner fills the width: the neighbours are not peeking in from the sides.
  final PageController _pages = PageController(initialPage: _start);
  int _page = _start;

  /// Runs from 0 to 1 while one banner is shown: the active dot fills with it, and when it is full the next banner
  /// slides in and the count starts again.
  late final AnimationController _clock = AnimationController(
    vsync: this,
    duration: _every,
  );

  @override
  void initState() {
    super.initState();
    _clock.addStatusListener((status) {
      if (status != AnimationStatus.completed) return;
      final count = ref.read(bannersProvider).value?.length ?? 0;
      if (count < 2 || !_pages.hasClients) {
        _clock.forward(from: 0);
        return;
      }
      _pages.animateToPage(
        _page + 1,
        duration: const Duration(milliseconds: 450),
        curve: Curves.easeInOut,
      );
    });
  }

  @override
  void dispose() {
    _clock.dispose();
    _pages.dispose();
    super.dispose();
  }

  /// A listing or a store opens inside the app; a full address opens in the browser; anything else does nothing.
  Future<void> _open(HomeBanner banner) async {
    final link = banner.linkUrl;
    if (link == null || link.isEmpty) return;
    if (link.startsWith('/listing/') || link.startsWith('/store/')) {
      context.push(link);
      return;
    }
    final uri = Uri.tryParse(link);
    if (uri != null && (uri.scheme == 'https' || uri.scheme == 'http')) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }

  @override
  Widget build(BuildContext context) {
    final banners = ref.watch(bannersProvider).value ?? const <HomeBanner>[];
    if (banners.isEmpty) return const SizedBox.shrink();
    final scheme = Theme.of(context).colorScheme;

    // The count only runs while there is something to move on to.
    if (banners.length > 1 && _clock.status == AnimationStatus.dismissed) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && _clock.status == AnimationStatus.dismissed) {
          _clock.forward();
        }
      });
    }

    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.md, bottom: AppSpacing.sm),
      child: Column(
        children: [
          AspectRatio(
            aspectRatio: 2.4,
            child: banners.length < 2
                ? Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.lg,
                    ),
                    child: _Slide(
                      banner: banners.first,
                      onTap: () => _open(banners.first),
                    ),
                  )
                : PageView.builder(
                    controller: _pages,
                    onPageChanged: (i) {
                      setState(() => _page = i);
                      _clock.forward(from: 0);
                    },
                    itemBuilder: (context, i) {
                      final banner = banners[i % banners.length];
                      return Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: AppSpacing.lg,
                        ),
                        child: _Slide(
                          banner: banner,
                          onTap: () => _open(banner),
                        ),
                      );
                    },
                  ),
          ),
          if (banners.length > 1)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var i = 0; i < banners.length; i++)
                    AnimatedContainer(
                      duration: const Duration(milliseconds: 450),
                      curve: Curves.easeInOutCubic,
                      margin: const EdgeInsets.symmetric(horizontal: 3),
                      width: i == _page % banners.length ? 22 : 6,
                      height: 6,
                      clipBehavior: Clip.antiAlias,
                      decoration: BoxDecoration(
                        color: scheme.outlineVariant,
                        borderRadius: BorderRadius.circular(3),
                      ),
                      child: i == _page % banners.length
                          ? AnimatedBuilder(
                              animation: _clock,
                              builder: (_, _) => Align(
                                alignment: Alignment.centerLeft,
                                child: FractionallySizedBox(
                                  widthFactor: Curves.easeInOutSine.transform(
                                    _clock.value,
                                  ),
                                  child: ColoredBox(color: scheme.primary),
                                ),
                              ),
                            )
                          : null,
                    ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _Slide extends StatelessWidget {
  const _Slide({required this.banner, required this.onTap});

  final HomeBanner banner;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return GestureDetector(
      onTap: onTap,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(AppRadius.lg),
        child: Stack(
          fit: StackFit.expand,
          children: [
            ColoredBox(color: scheme.surfaceContainerHighest),
            CachedNetworkImage(
              imageUrl: banner.imageUrl,
              fit: BoxFit.cover,
              errorWidget: (_, _, _) => const SizedBox.shrink(),
            ),
            const DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.bottomCenter,
                  end: Alignment.center,
                  colors: [Color(0xB3000000), Color(0x00000000)],
                ),
              ),
            ),
            Positioned(
              left: 14,
              right: 14,
              bottom: 12,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    banner.title,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 17,
                      fontWeight: FontWeight.w800,
                      height: 1.15,
                    ),
                  ),
                  if (banner.subtitle != null && banner.subtitle!.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Text(
                        banner.subtitle!,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 13,
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
