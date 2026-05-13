using System;
using System.Collections.Generic;

namespace Oracle_Health.Models;

public partial class Schedule
{
    public long Id { get; set; }

    public long DoctorId { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool? IsOnLeave { get; set; }

    public long? AppointmentId { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;
}
