// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Spanish Castilian (`es`).
class AppLocalizationsEs extends AppLocalizations {
  AppLocalizationsEs([String locale = 'es']) : super(locale);

  @override
  String get appTitle => 'InitialAspire';

  @override
  String get login => 'Iniciar sesión';

  @override
  String get logout => 'Cerrar sesión';

  @override
  String get register => 'Registrarse';

  @override
  String get email => 'Correo electrónico';

  @override
  String get password => 'Contraseña';

  @override
  String get fullName => 'Nombre completo';

  @override
  String get forgotPassword => '¿Olvidó su contraseña?';

  @override
  String get resetPassword => 'Restablecer contraseña';

  @override
  String get confirmEmail => 'Confirmar correo';

  @override
  String get resendActivation => 'Reenviar enlace de activación';

  @override
  String get newPassword => 'Nueva contraseña';

  @override
  String get confirmPassword => 'Confirmar contraseña';

  @override
  String get currentPassword => 'Contraseña actual';

  @override
  String get changePassword => 'Cambiar contraseña';

  @override
  String get save => 'Guardar';

  @override
  String get cancel => 'Cancelar';

  @override
  String get profile => 'Perfil';

  @override
  String get weather => 'Clima';

  @override
  String get loading => 'Cargando...';

  @override
  String get errorServer => 'Error del servidor. Intente nuevamente.';

  @override
  String get errorNetwork => 'Sin conexión a internet.';

  @override
  String get errorUnauthorized => 'Sesión expirada. Inicie sesión nuevamente.';

  @override
  String get errorUnknown => 'Ocurrió un error inesperado.';

  @override
  String get errorEmailNotConfirmed =>
      'Correo no confirmado. Revise su bandeja de entrada.';

  @override
  String get loginSuccess => '¡Inicio de sesión exitoso!';

  @override
  String get registerSuccess =>
      '¡Registrado! Revise su correo para activar su cuenta.';

  @override
  String get forgotPasswordSuccess =>
      'Si el correo existe, se envió un enlace de restablecimiento.';

  @override
  String get resetPasswordSuccess => '¡Contraseña restablecida exitosamente!';

  @override
  String get confirmEmailSuccess => '¡Correo confirmado exitosamente!';

  @override
  String get resendActivationSuccess => 'Enlace de activación reenviado.';

  @override
  String get profileUpdateSuccess => '¡Perfil actualizado exitosamente!';

  @override
  String get changePasswordSuccess => '¡Contraseña cambiada exitosamente!';

  @override
  String get fieldRequired => 'Campo obligatorio';

  @override
  String get invalidEmail => 'Correo inválido';

  @override
  String get passwordTooShort =>
      'La contraseña debe tener al menos 6 caracteres';

  @override
  String get passwordsDoNotMatch => 'Las contraseñas no coinciden';

  @override
  String get noAccount => '¿No tiene cuenta?';

  @override
  String get alreadyHaveAccount => '¿Ya tiene cuenta?';

  @override
  String get sendResetLink => 'Enviar enlace de restablecimiento';

  @override
  String get date => 'Fecha';

  @override
  String get temperatureC => 'Temperatura (°C)';

  @override
  String get temperatureF => 'Temperatura (°F)';

  @override
  String get summary => 'Resumen';

  @override
  String get roles => 'Roles';

  @override
  String get retry => 'Reintentar';
}
