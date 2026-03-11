namespace InitialAspireProject.Web.Services
{
    public class RegisterResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class ForgotPasswordResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class ResetPasswordResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class ConfirmEmailResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
