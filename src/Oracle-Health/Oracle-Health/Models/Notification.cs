using System;
using System.Collections.Generic;

namespace Oracle_Health.Models;

public partial class Notification
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
