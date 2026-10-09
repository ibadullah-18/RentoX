import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/domain/auth_models.dart';
import '../features/auth/presentation/auth_controller.dart';
import '../features/auth/presentation/phone_page.dart';
import '../features/auth/presentation/verify_page.dart';
import '../features/favorites/presentation/favorites_page.dart';
import '../features/home/presentation/home_page.dart';
import '../features/listing_create/presentation/create_listing_page.dart';
import '../features/listing_details/presentation/listing_details_page.dart';
import '../features/my_listings/presentation/edit_listing_page.dart';
import '../features/my_listings/presentation/my_listing_details_page.dart';
import '../features/my_listings/presentation/my_listings_page.dart';
import '../features/messages/presentation/chat_page.dart';
import '../features/messages/presentation/messages_page.dart';
import '../features/notifications/presentation/notifications_page.dart';
import '../features/search/presentation/search_page.dart';
import '../features/store/presentation/followed_stores_page.dart';
import '../features/support/domain/support_models.dart';
import '../features/support/presentation/new_ticket_page.dart';
import '../features/support/presentation/support_page.dart';
import '../features/support/presentation/ticket_page.dart';
import '../features/store/presentation/my_store_page.dart';
import '../features/store/presentation/store_form_page.dart';
import '../features/store/presentation/store_page.dart';
import '../features/profile/presentation/edit_profile_page.dart';
import '../features/profile/presentation/profile_page.dart';
import '../features/shell/app_shell.dart';
import '../shared/widgets/motion.dart';

abstract final class Routes {
  static const home = '/';
  static const search = '/search';
  static const favorites = '/favorites';
  static const create = '/create';
  static const messages = '/messages';
  static const profile = '/profile';
  static const profileEdit = '/profile/edit';
  static const support = '/support';

  /// A support request: `/support/<id>`.
  static String ticket(String id) => '/support/$id';

  /// The "new request" form, optionally pre-filled (opened from a listing or
  /// a store): `/support/new?category=3&subject=...&ref=...`.
  static String newTicket({int? category, String? subject, String? ref}) => Uri(
    path: '/support/new',
    queryParameters: {
      if (category != null) 'category': '$category',
      if (subject != null && subject.isNotEmpty) 'subject': subject,
      if (ref != null && ref.isNotEmpty) 'ref': ref,
    },
  ).toString();

  static const myStore = '/store/mine';
  static const myStoreEdit = '/store/mine/edit';
  static const followedStores = '/store/following';

  /// A public store page: `/store/<slug>`.
  static String store(String slug) => '/store/$slug';
  static const notifications = '/notifications';
  static const myListings = '/my-listings';

  /// Edit a draft or rejected listing: `/my-listings/<id>/edit`.
  static String editListing(String id) => '/my-listings/$id/edit';

  /// The owner's view of one of their listings: `/my-listings/<id>`.
  static String myListing(String id) => '/my-listings/$id';

  /// One conversation: `/messages/<id>`.
  static String chat(String id) => '/messages/$id';

  /// Listing details: `/listing/<id>`.
  static String listing(String id) => '/listing/$id';

  /// Search, optionally pre-filled: `/search?q=toyota&categoryId=...`.
  static String searchWith({
    String? query,
    String? categoryId,
    bool focus = false,
  }) => Uri(
    path: search,
    queryParameters: {
      if (query != null && query.isNotEmpty) 'q': query,
      'categoryId': ?categoryId,
      if (focus) 'focus': '1',
    },
  ).toString();
  static const phone = '/auth/phone';
  static const verify = '/auth/verify';

  /// Browsing is public; these need an account.
  static bool isProtected(String path) =>
      _protected.contains(path) ||
      path.startsWith('$myListings/') ||
      path.startsWith('$messages/') ||
      path.startsWith('$profile/') ||
      path == followedStores ||
      path == support ||
      path.startsWith('$support/') ||
      path == myStore ||
      path.startsWith('$myStore/');

  static const _protected = {
    myListings,
    favorites,
    create,
    messages,
    profile,
    notifications,
  };
}

final routerProvider = Provider<GoRouter>((ref) {
  final refresh = ValueNotifier<int>(0);
  ref.listen(authControllerProvider, (_, _) => refresh.value++);
  ref.onDispose(refresh.dispose);

  return GoRouter(
    initialLocation: Routes.home,
    refreshListenable: refresh,
    redirect: (context, state) {
      final auth = ref.read(authControllerProvider);
      if (auth.isLoading) return null;

      final signedIn = auth.value != null;
      final path = state.uri.path;

      if (!signedIn && Routes.isProtected(path)) {
        return Uri(
          path: Routes.phone,
          queryParameters: {'from': state.uri.toString()},
        ).toString();
      }
      if (signedIn && path.startsWith('/auth/')) return Routes.home;
      return null;
    },
    routes: [
      GoRoute(
        path: Routes.phone,
        builder: (_, state) =>
            PhonePage(redirectTo: state.uri.queryParameters['from']),
      ),
      GoRoute(
        path: Routes.verify,
        redirect: (_, state) =>
            state.extra is OtpFlowArgs ? null : Routes.phone,
        builder: (_, state) => VerifyPage(
          args: state.extra! as OtpFlowArgs,
          redirectTo: state.uri.queryParameters['from'],
        ),
      ),
      // Full-screen (outside the tab bar); reached from the home search bar.
      GoRoute(
        path: Routes.search,
        builder: (_, state) => SearchPage(
          initialQuery: state.uri.queryParameters['q'] ?? '',
          initialCategoryId: state.uri.queryParameters['categoryId'],
          autofocus: state.uri.queryParameters['focus'] == '1',
        ),
      ),
      GoRoute(
        path: '/listing/:id',
        builder: (_, state) =>
            ListingDetailsPage(listingId: state.pathParameters['id']!),
      ),
      // Full-screen flows (no tab bar).
      GoRoute(
        path: Routes.create,
        builder: (_, _) => const CreateListingPage(),
      ),
      GoRoute(
        path: Routes.myListings,
        builder: (_, _) => const MyListingsPage(),
      ),
      GoRoute(
        path: '/my-listings/:id',
        builder: (_, state) =>
            MyListingDetailsPage(listingId: state.pathParameters['id']!),
        routes: [
          GoRoute(
            path: 'edit',
            builder: (_, state) =>
                EditListingPage(listingId: state.pathParameters['id']!),
          ),
        ],
      ),
      GoRoute(
        path: '/messages/:id',
        builder: (_, state) =>
            ChatPage(conversationId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: Routes.support,
        builder: (_, _) => const SupportPage(),
        routes: [
          // `new` must come before `:id`.
          GoRoute(
            path: 'new',
            builder: (_, state) {
              final q = state.uri.queryParameters;
              return NewTicketPage(
                category: SupportCategory.fromId(
                  int.tryParse(q['category'] ?? '') ?? 1,
                ),
                subject: q['subject'] ?? '',
                reference: q['ref'] ?? '',
              );
            },
          ),
          GoRoute(
            path: ':id',
            builder: (_, state) =>
                TicketPage(ticketId: state.pathParameters['id']!),
          ),
        ],
      ),
      // Static `/store/...` routes first, then the public `/store/:slug`.
      GoRoute(
        path: Routes.myStore,
        builder: (_, _) => const MyStorePage(),
        routes: [
          GoRoute(path: 'edit', builder: (_, _) => const StoreFormPage()),
        ],
      ),
      GoRoute(
        path: Routes.followedStores,
        builder: (_, _) => const FollowedStoresPage(),
      ),
      GoRoute(
        path: '/store/:slug',
        builder: (_, state) => StorePage(slug: state.pathParameters['slug']!),
      ),
      GoRoute(
        path: Routes.profileEdit,
        builder: (_, _) => const EditProfilePage(),
      ),
      GoRoute(
        path: Routes.notifications,
        builder: (_, _) => const NotificationsPage(),
      ),
      StatefulShellRoute(
        builder: (_, _, shell) => AppShell(navigationShell: shell),
        navigatorContainerBuilder: (context, shell, children) =>
            FadeBranchContainer(
              currentIndex: shell.currentIndex,
              children: children,
            ),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(path: Routes.home, builder: (_, _) => const HomePage()),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: Routes.favorites,
                builder: (_, _) => const FavoritesPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: Routes.messages,
                builder: (_, _) => const MessagesPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: Routes.profile,
                builder: (_, _) => const ProfilePage(),
              ),
            ],
          ),
        ],
      ),
    ],
  );
});
