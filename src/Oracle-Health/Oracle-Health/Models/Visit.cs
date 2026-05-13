using System;
using System.Collections.Generic;

namespace Oracle_Health.Models;

public partial class Visit
{
    public long Id { get; set; }

    public long PatientId { get; set; }

    public long DoctorId { get; set; }

    public long AppointmentId { get; set; }

    public string Notes { get; set; } = null!;

    public string? Prescription { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}
