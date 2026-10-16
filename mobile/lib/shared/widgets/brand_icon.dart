import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';

import 'brand_paths.dart';

/// The networks a store can link to, each with its real logo and brand colour.
enum Brand {
  instagram(BrandPaths.instagram, Color(0xFFE4405F)),
  facebook(BrandPaths.facebook, Color(0xFF1877F2)),
  tiktok(BrandPaths.tikTok, Color(0xFF111111)),
  youtube(BrandPaths.youTube, Color(0xFFFF0000)),
  whatsapp(BrandPaths.whatsApp, Color(0xFF25D366));

  const Brand(this.path, this.color);

  final String path;
  final Color color;
}

/// A network's logo, drawn in [color] (the brand colour unless told otherwise).
class BrandIcon extends StatelessWidget {
  const BrandIcon(this.brand, {super.key, this.size = 22, this.color});

  final Brand brand;
  final double size;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final svg =
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">'
        '<path d="${brand.path}"/></svg>';
    return SvgPicture.string(
      svg,
      width: size,
      height: size,
      colorFilter: ColorFilter.mode(color ?? brand.color, BlendMode.srcIn),
    );
  }
}
