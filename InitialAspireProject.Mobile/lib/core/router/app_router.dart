import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/application/providers/auth_state_provider.dart';
import '../../features/auth/presentation/screens/confirm_email_screen.dart';
import '../../features/auth/presentation/screens/forgot_password_screen.dart';
import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/auth/presentation/screens/register_screen.dart';
import '../../features/auth/presentation/screens/reset_password_screen.dart';
import '../../features/profile/presentation/screens/profile_screen.dart';
import '../../features/weather/presentation/screens/weather_screen.dart';
import '../../shared/widgets/app_scaffold.dart';
import 'router_refresh.dart';

final _rootNavigatorKey = GlobalKey<NavigatorState>();
final _shellNavigatorKey = GlobalKey<NavigatorState>();

const _publicRoutes = [
  '/login',
  '/register',
  '/forgot-password',
  '/reset-password',
  '/confirm-email',
];

final routerProvider = Provider<GoRouter>((ref) {
  final refreshNotifier = RouterRefresh(ref, authStatusProvider);

  return GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: '/weather',
    refreshListenable: refreshNotifier,
    redirect: (context, state) {
      final authStatus = ref.read(authStatusProvider);
      final currentPath = state.matchedLocation;

      if (authStatus == AuthStatus.unknown) return null;

      final isPublic = _publicRoutes.any((r) => currentPath.startsWith(r));
      final isAuthenticated = authStatus == AuthStatus.authenticated;

      if (!isAuthenticated && !isPublic) return '/login';
      if (isAuthenticated && currentPath == '/login') return '/weather';

      return null;
    },
    routes: [
      // Public routes
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: '/forgot-password',
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: '/reset-password',
        builder: (context, state) {
          final email = state.uri.queryParameters['email'] ?? '';
          final token = state.uri.queryParameters['token'] ?? '';
          return ResetPasswordScreen(email: email, token: token);
        },
      ),
      GoRoute(
        path: '/confirm-email',
        builder: (context, state) {
          final email = state.uri.queryParameters['email'] ?? '';
          final token = state.uri.queryParameters['token'] ?? '';
          return ConfirmEmailScreen(email: email, token: token);
        },
      ),

      // Authenticated shell
      ShellRoute(
        navigatorKey: _shellNavigatorKey,
        builder: (context, state, child) => AppScaffold(child: child),
        routes: [
          GoRoute(
            path: '/weather',
            builder: (context, state) => const WeatherScreen(),
          ),
          GoRoute(
            path: '/profile',
            builder: (context, state) => const ProfileScreen(),
          ),
        ],
      ),
    ],
  );
});
