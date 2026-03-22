// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'InitialAspire';

  @override
  String get login => 'Login';

  @override
  String get logout => 'Logout';

  @override
  String get register => 'Register';

  @override
  String get email => 'Email';

  @override
  String get password => 'Password';

  @override
  String get fullName => 'Full name';

  @override
  String get forgotPassword => 'Forgot password?';

  @override
  String get resetPassword => 'Reset password';

  @override
  String get confirmEmail => 'Confirm email';

  @override
  String get resendActivation => 'Resend activation link';

  @override
  String get newPassword => 'New password';

  @override
  String get confirmPassword => 'Confirm password';

  @override
  String get currentPassword => 'Current password';

  @override
  String get changePassword => 'Change password';

  @override
  String get save => 'Save';

  @override
  String get cancel => 'Cancel';

  @override
  String get profile => 'Profile';

  @override
  String get weather => 'Weather';

  @override
  String get loading => 'Loading...';

  @override
  String get errorServer => 'Server error. Please try again.';

  @override
  String get errorNetwork => 'No internet connection.';

  @override
  String get errorUnauthorized => 'Session expired. Please log in again.';

  @override
  String get errorUnknown => 'An unexpected error occurred.';

  @override
  String get errorEmailNotConfirmed => 'Email not confirmed. Check your inbox.';

  @override
  String get loginSuccess => 'Logged in successfully!';

  @override
  String get registerSuccess =>
      'Registered! Check your email to activate your account.';

  @override
  String get forgotPasswordSuccess =>
      'If the email exists, a reset link has been sent.';

  @override
  String get resetPasswordSuccess => 'Password reset successfully!';

  @override
  String get confirmEmailSuccess => 'Email confirmed successfully!';

  @override
  String get resendActivationSuccess => 'Activation link resent.';

  @override
  String get profileUpdateSuccess => 'Profile updated successfully!';

  @override
  String get changePasswordSuccess => 'Password changed successfully!';

  @override
  String get fieldRequired => 'Required field';

  @override
  String get invalidEmail => 'Invalid email';

  @override
  String get passwordTooShort => 'Password must be at least 6 characters';

  @override
  String get passwordsDoNotMatch => 'Passwords do not match';

  @override
  String get noAccount => 'Don\'t have an account?';

  @override
  String get alreadyHaveAccount => 'Already have an account?';

  @override
  String get sendResetLink => 'Send reset link';

  @override
  String get date => 'Date';

  @override
  String get temperatureC => 'Temperature (°C)';

  @override
  String get temperatureF => 'Temperature (°F)';

  @override
  String get summary => 'Summary';

  @override
  String get roles => 'Roles';

  @override
  String get retry => 'Retry';
}
