// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Portuguese (`pt`).
class AppLocalizationsPt extends AppLocalizations {
  AppLocalizationsPt([String locale = 'pt']) : super(locale);

  @override
  String get appTitle => 'InitialAspire';

  @override
  String get login => 'Entrar';

  @override
  String get logout => 'Sair';

  @override
  String get register => 'Cadastrar';

  @override
  String get email => 'E-mail';

  @override
  String get password => 'Senha';

  @override
  String get fullName => 'Nome completo';

  @override
  String get forgotPassword => 'Esqueceu a senha?';

  @override
  String get resetPassword => 'Redefinir senha';

  @override
  String get confirmEmail => 'Confirmar e-mail';

  @override
  String get resendActivation => 'Reenviar link de ativação';

  @override
  String get newPassword => 'Nova senha';

  @override
  String get confirmPassword => 'Confirmar senha';

  @override
  String get currentPassword => 'Senha atual';

  @override
  String get changePassword => 'Alterar senha';

  @override
  String get save => 'Salvar';

  @override
  String get cancel => 'Cancelar';

  @override
  String get profile => 'Perfil';

  @override
  String get weather => 'Clima';

  @override
  String get loading => 'Carregando...';

  @override
  String get errorServer => 'Erro no servidor. Tente novamente.';

  @override
  String get errorNetwork => 'Sem conexão com a internet.';

  @override
  String get errorUnauthorized => 'Sessão expirada. Faça login novamente.';

  @override
  String get errorUnknown => 'Ocorreu um erro inesperado.';

  @override
  String get errorEmailNotConfirmed =>
      'E-mail não confirmado. Verifique sua caixa de entrada.';

  @override
  String get loginSuccess => 'Login realizado com sucesso!';

  @override
  String get registerSuccess =>
      'Cadastro realizado! Verifique seu e-mail para ativar a conta.';

  @override
  String get forgotPasswordSuccess =>
      'Se o e-mail existir, um link de redefinição foi enviado.';

  @override
  String get resetPasswordSuccess => 'Senha redefinida com sucesso!';

  @override
  String get confirmEmailSuccess => 'E-mail confirmado com sucesso!';

  @override
  String get resendActivationSuccess => 'Link de ativação reenviado.';

  @override
  String get profileUpdateSuccess => 'Perfil atualizado com sucesso!';

  @override
  String get changePasswordSuccess => 'Senha alterada com sucesso!';

  @override
  String get fieldRequired => 'Campo obrigatório';

  @override
  String get invalidEmail => 'E-mail inválido';

  @override
  String get passwordTooShort => 'A senha deve ter pelo menos 6 caracteres';

  @override
  String get passwordsDoNotMatch => 'As senhas não coincidem';

  @override
  String get noAccount => 'Não tem conta?';

  @override
  String get alreadyHaveAccount => 'Já tem conta?';

  @override
  String get sendResetLink => 'Enviar link de redefinição';

  @override
  String get date => 'Data';

  @override
  String get temperatureC => 'Temperatura (°C)';

  @override
  String get temperatureF => 'Temperatura (°F)';

  @override
  String get summary => 'Resumo';

  @override
  String get roles => 'Funções';

  @override
  String get retry => 'Tentar novamente';
}
