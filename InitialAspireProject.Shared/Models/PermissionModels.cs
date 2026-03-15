namespace InitialAspireProject.Shared.Models
{
    public class RolePermissionsDto
    {
        public required string RoleName { get; set; }
        public List<string> Permissions { get; set; } = [];
    }

    public class AssignPermissionModel
    {
        public required string Permission { get; set; }
    }
}
