using System;
using System.Collections.Generic;

namespace Oracle_Health.Models;

public partial class Appointment
{
    public long Id { get; set; }

    public long PatientId { get; set; }

    public long DoctorId { get; set; }

    public DateTime Date { get; set; }

    public int DurationMinutes { get; set; }

    public int Status { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual Visit? Visit { get; set; }
}
