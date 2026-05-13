using System;
using System.Collections.Generic;

namespace Oracle_Health.Models;

public partial class Patient
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long PatientId { get; set; }

    public long Cpr { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
