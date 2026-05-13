using System;
using System.Collections.Generic;

namespace Oracle_Health.Models;

public partial class Specialization
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
