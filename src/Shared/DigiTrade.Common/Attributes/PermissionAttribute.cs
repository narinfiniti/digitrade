namespace DigiTrade.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PermissionAttribute : Attribute
{
    public string Permission { get; set; }
    /// <summary>
    /// </summary>
    /// <param name="permission">The permission</param>
    public PermissionAttribute(string permission)
    {
        Permission = permission;
    }
}