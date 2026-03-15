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

        public record PermissionInfo(string Key, string Category);

        public static readonly PermissionInfo[] AllPermissions =
        [
            new(CanViewSettings, "System.Settings"),
            new(CanManageUsers, "System.Users"),
            new(CanViewReports, "System.Reports"),
            new(CanManagePermissions, "System.Security")
        ];
    }
}
