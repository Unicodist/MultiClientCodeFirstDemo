namespace MultiTenantDemo.Client.Domain;

public sealed record CustomerSummary(int TotalCustomers, int ActiveCustomers, int CreatedSince, DateTime? LatestCreatedUtc);
