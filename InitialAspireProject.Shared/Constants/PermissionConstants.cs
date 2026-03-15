namespace InitialAspireProject.Shared.Constants
{
    public static class PermissionConstants
    {
        public const string ClaimType = "Permission";

        public const string CanViewSettings = "CanViewSettings";
        public const string CanManageUsers = "CanManageUsers";
        public const string CanViewReports = "CanViewReports";
        public const string CanManagePermissions = "CanManagePermissions";

        public static readonly string[] All =
        [
            CanViewSettings,
            CanManageUsers,
            CanViewReports,
            CanManagePermissions
        ];
    }
}
