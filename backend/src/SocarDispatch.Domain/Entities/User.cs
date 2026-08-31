using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public RoleType RoleType { get; set; } // Employee, Team, Operator
    public string? SubRole { get; set; }
    public string? AvatarUrl { get; set; }
    public string? DeviceToken { get; set; }
    public DateTime? DeviceTokenUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<Team> LedTeams { get; set; } = new List<Team>(); // Teams he has led
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public ICollection<Incident> ReportedIncidents { get; set; } = new List<Incident>();
    public ICollection<Assignment> AssignedDispatches { get; set; } = new List<Assignment>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

}
