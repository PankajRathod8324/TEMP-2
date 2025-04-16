using System;
using System.Collections.Generic;

namespace Entities.Models;

public partial class CustomerTable
{
    public int CustomerTableId { get; set; }

    public int? CustomerId { get; set; }

    public int? TableId { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Table? Table { get; set; }
}
