using System.Globalization;
using System.Resources;

namespace InitialAspireProject.Web.Resources;

public class WebMessages
{
    private static ResourceManager? _resourceManager;

    private static ResourceManager ResourceManager =>
        _resourceManager ??= new ResourceManager(
            "InitialAspireProject.Web.Resources.WebMessages",
            typeof(WebMessages).Assembly);

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    // Validation messages — used by DataAnnotations ErrorMessageResourceType
    public static string UsernameRequired => Get(nameof(UsernameRequired));
    public static string PasswordRequired => Get(nameof(PasswordRequired));
    public static string NameRequired => Get(nameof(NameRequired));
    public static string EmailRequired => Get(nameof(EmailRequired));
    public static string InvalidEmail => Get(nameof(InvalidEmail));
    public static string PasswordMinLength => Get(nameof(PasswordMinLength));
    public static string PasswordMin8 => Get(nameof(PasswordMin8));
    public static string ConfirmPasswordRequired => Get(nameof(ConfirmPasswordRequired));
    public static string PasswordMismatch => Get(nameof(PasswordMismatch));
    public static string MustAcceptTerms => Get(nameof(MustAcceptTerms));
    public static string NewPasswordRequired => Get(nameof(NewPasswordRequired));
    public static string CurrentPasswordRequired => Get(nameof(CurrentPasswordRequired));

    // Display names — used by DataAnnotations Display(ResourceType)
    public static string FullName => Get(nameof(FullName));
    public static string CurrentPassword => Get(nameof(CurrentPassword));
    public static string NewPassword => Get(nameof(NewPassword));
    public static string ConfirmNewPassword => Get(nameof(ConfirmNewPassword));
}
