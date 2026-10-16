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
  static const IconData schedule = Symbols.schedule_rounded;
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
  static const IconData appearance = Symbols.contrast_rounded;
  static const IconData share = Symbols.ios_share_rounded;
  static const IconData devices = Symbols.devices_rounded;
  static const IconData edit = Symbols.edit_rounded;
  static const IconData store = Symbols.storefront_rounded;
  static const IconData mail = Symbols.mail_rounded;
  static const IconData place = Symbols.location_on_rounded;
  static const IconData link = Symbols.link_rounded;
  static const IconData follow = Symbols.person_add_rounded;
  static const IconData following = Symbols.how_to_reg_rounded;
  static const IconData support = Symbols.support_agent_rounded;
  static const IconData info = Symbols.info_rounded;
  static const IconData phone = Symbols.call_rounded;
  static const IconData hotline = Symbols.headset_mic_rounded;
  static const IconData music = Symbols.music_note_rounded;
  static const IconData group = Symbols.group_rounded;
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

  /// The icons an admin can give a category (the web panel stores the key in
  /// the category's iconUrl). The same keys exist on the website.
  static const Map<String, IconData> byKey = {
    'car': Symbols.directions_car_rounded,
    'bike': Symbols.two_wheeler_rounded,
    'bus': Symbols.directions_bus_rounded,
    'truck': Symbols.local_shipping_rounded,
    'boat': Symbols.directions_boat_rounded,
    'plane': Symbols.flight_rounded,
    'building': Symbols.apartment_rounded,
    'home': Symbols.home_rounded,
    'key': Symbols.key_rounded,
    'furniture': Symbols.chair_rounded,
    'garden': Symbols.yard_rounded,
    'tools': Symbols.handyman_rounded,
    'repair': Symbols.build_rounded,
    'event': Symbols.festival_rounded,
    'camera': Symbols.photo_camera_rounded,
    'laptop': Symbols.laptop_mac_rounded,
    'phone': Symbols.smartphone_rounded,
    'tv': Symbols.tv_rounded,
    'music': Symbols.music_note_rounded,
    'games': Symbols.sports_esports_rounded,
    'sport': Symbols.fitness_center_rounded,
    'clothes': Symbols.checkroom_rounded,
    'baby': Symbols.child_care_rounded,
    'pets': Symbols.pets_rounded,
    'books': Symbols.menu_book_rounded,
    'luggage': Symbols.luggage_rounded,
  };

  /// The icon of a category: the one the admin chose ([iconKey]), else a
  /// guess from the slug.
  static IconData categoryIcon(String slug, {String? iconKey}) {
    final chosen = iconKey == null ? null : byKey[iconKey.trim()];
    if (chosen != null) return chosen;

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
