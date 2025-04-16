using System;
using System.Collections.Generic;

namespace Entities.Models;

public partial class RoleAndPermission
{
    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ModifiedBy { get; set; }

    public DateTime ModifiedAt { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
