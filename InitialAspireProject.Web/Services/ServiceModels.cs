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

    public class ErrorValidation
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
    }
}
