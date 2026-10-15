namespace MultiTenantDemo.Client.Domain;

public class Customer
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 320;

    private Customer()
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    public Customer(string name, string email, DateTime createdUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        CreatedUtc = createdUtc;
        IsActive = true;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public void Deactivate() => IsActive = false;
}
