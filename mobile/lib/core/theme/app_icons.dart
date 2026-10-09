import 'package:flutter/widgets.dart';
import 'package:material_symbols_icons/symbols.dart';

/// The single icon vocabulary of the app: Material Symbols *Rounded*, drawn
/// at weight 300 (thin, soft) via the global IconTheme. Active/selected states
/// use the same glyph with `fill: 1` (see [AppIcons.fillOf]).
/// Never reference icon packs directly from widgets; add a name here.
abstract final class AppIcons {
  /// Variable-font "fill" axis: 1 for active/selected, 0 otherwise.
  static double fillOf(bool active) => active ? 1 : 0;

  // Navigation
  static const IconData home = Symbols.home_rounded;
  static const IconData search = Symbols.search_rounded;
  static const IconData add = Symbols.add_rounded;
  static const IconData messages = Symbols.chat_bubble_rounded;
  static const IconData profile = Symbols.person_rounded;

  // Actions
  static const IconData bell = Symbols.notifications_rounded;
  static const IconData filter = Symbols.tune_rounded;
  static const IconData heart = Symbols.favorite_rounded;
  static const IconData back = Symbols.chevron_left_rounded;
  static const IconData forward = Symbols.chevron_right_rounded;
  static const IconData signOut = Symbols.logout_rounded;
  static const IconData close = Symbols.close_rounded;
  static const IconData call = Symbols.call_rounded;
  static const IconData send = Symbols.send_rounded;
  static const IconData history = Symbols.history_rounded;
  static const IconData view = Symbols.visibility_rounded;
  static const IconData time = Symbols.schedule_rounded;
  static const IconData person = Symbols.person_rounded;
  static const IconData camera = Symbols.photo_camera_rounded;
  static const IconData addPhoto = Symbols.add_photo_alternate_rounded;
  static const IconData cover = Symbols.star_rounded;
  static const IconData trash = Symbols.delete_rounded;
  static const IconData check = Symbols.check_circle_rounded;
  static const IconData error = Symbols.error_rounded;
  static const IconData wallet = Symbols.account_balance_wallet_rounded;
  static const IconData listings = Symbols.list_alt_rounded;
  static const IconData more = Symbols.more_vert_rounded;
  static const IconData block = Symbols.block_rounded;
  static const IconData report = Symbols.flag_rounded;
  static const IconData retry = Symbols.refresh_rounded;
  static const IconData sent = Symbols.done_rounded;
  static const IconData read = Symbols.done_all_rounded;
  static const IconData lock = Symbols.lock_rounded;
  static const IconData language = Symbols.language_rounded;
  static const IconData devices = Symbols.devices_rounded;
  static const IconData edit = Symbols.edit_rounded;
  static const IconData store = Symbols.storefront_rounded;
  static const IconData support = Symbols.support_agent_rounded;
  static const IconData info = Symbols.info_rounded;
  static const IconData bellOff = Symbols.notifications_off_rounded;
  static const IconData chatEmpty = Symbols.forum_rounded;

  // Status / trust
  static const IconData vip = Symbols.workspace_premium_rounded;
  static const IconData verified = Symbols.verified_rounded;
  static const IconData shield = Symbols.verified_user_rounded;
  static const IconData offline = Symbols.wifi_off_rounded;
  static const IconData empty = Symbols.inbox_rounded;
  static const IconData image = Symbols.image_rounded;
  static const IconData soon = Symbols.hourglass_top_rounded;

  // Categories (matched by slug keywords; see [categoryIcon]).
  static const IconData categoryDefault = Symbols.grid_view_rounded;
  static const IconData realEstate = Symbols.apartment_rounded;
  static const IconData vehicle = Symbols.directions_car_rounded;
  static const IconData motorbike = Symbols.two_wheeler_rounded;
  static const IconData equipment = Symbols.local_shipping_rounded;
  static const IconData event = Symbols.festival_rounded;
  static const IconData tools = Symbols.build_rounded;

  static IconData categoryIcon(String slug) {
    final s = slug.toLowerCase();
    bool has(List<String> keys) => keys.any(s.contains);

    if (has(['moto', 'bike', 'scooter'])) return motorbike;
    if (has(['car', 'auto', 'vehicle', 'neqliyyat', 'transport'])) {
      return vehicle;
    }
    if (has(['apartment', 'home', 'house', 'real', 'menzil', 'villa'])) {
      return realEstate;
    }
    if (has(['equip', 'machin', 'texnika', 'construction'])) return equipment;
    if (has(['event', 'tedbir', 'tent'])) return event;
    if (has(['tool', 'alet'])) return tools;
    return categoryDefault;
  }
}
