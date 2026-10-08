import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// The user's last search phrases (newest first), stored on the device.
class RecentSearches extends Notifier<List<String>> {
  static const _key = 'recent_searches';
  static const maxItems = 8;

  @override
  List<String> build() {
    _load();
    return const [];
  }

  Future<void> _load() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      state = prefs.getStringList(_key) ?? const [];
    } catch (_) {
      // Storage unavailable: recents simply stay empty.
    }
  }

  Future<void> add(String phrase) async {
    final value = phrase.trim();
    if (value.length < 2) return;
    state = [
      value,
      ...state.where((e) => e.toLowerCase() != value.toLowerCase()),
    ].take(maxItems).toList(growable: false);
    await _save();
  }

  Future<void> clear() async {
    state = const [];
    await _save();
  }

  Future<void> _save() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setStringList(_key, state);
    } catch (_) {
      // Best effort.
    }
  }
}

final recentSearchesProvider = NotifierProvider<RecentSearches, List<String>>(
  RecentSearches.new,
);
