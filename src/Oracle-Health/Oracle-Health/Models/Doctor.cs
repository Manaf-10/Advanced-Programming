using System;
using System.Collections.Generic;

namespace Oracle_Health.Models;

public partial class Doctor
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public virtual ICollection<Specialization> Specializations { get; set; } = new List<Specialization>();
}
