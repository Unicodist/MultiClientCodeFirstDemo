CREATE OR ALTER PROCEDURE dbo.GetCustomerSummary
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT_BIG(*) AS TotalCustomers,
        SUM(CASE WHEN c.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCustomers,
        MAX(c.CreatedUtc) AS LatestCreatedUtc
    FROM dbo.Customers AS c;
END
