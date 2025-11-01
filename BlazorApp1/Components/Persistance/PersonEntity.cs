using System;

namespace BlazorApp1.Data;

public class PersonEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}