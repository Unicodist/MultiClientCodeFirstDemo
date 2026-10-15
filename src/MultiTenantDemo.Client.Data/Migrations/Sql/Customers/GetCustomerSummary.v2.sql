CREATE OR ALTER PROCEDURE dbo.GetCustomerSummary
    @CreatedSinceUtc datetime2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS TotalCustomers,
        ISNULL(SUM(CASE WHEN c.IsActive = 1 THEN 1 ELSE 0 END), 0) AS ActiveCustomers,
        ISNULL(SUM(CASE WHEN @CreatedSinceUtc IS NOT NULL AND c.CreatedUtc >= @CreatedSinceUtc THEN 1 ELSE 0 END), 0) AS CreatedSince,
        MAX(c.CreatedUtc) AS LatestCreatedUtc
    FROM dbo.Customers AS c;
END
