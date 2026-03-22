class ApiConstants {
  ApiConstants._();

  // Auth
  static const login = '/api/auth/login';
  static const register = '/api/auth/register';
  static const refresh = '/api/auth/refresh';
  static const revoke = '/api/auth/revoke';
  static const forgotPassword = '/api/auth/forgot-password';
  static const resetPassword = '/api/auth/reset-password';
  static const confirmEmail = '/api/auth/confirm-email';
  static const resendActivation = '/api/auth/resend-activation';

  // Profile
  static const profile = '/api/profile';
  static const changePassword = '/api/profile/change-password';

  // Weather
  static const weather = '/api/weather';

  /// Paths that do not require Bearer token.
  static const publicPaths = [
    login,
    register,
    refresh,
    forgotPassword,
    resetPassword,
    confirmEmail,
    resendActivation,
  ];
}
