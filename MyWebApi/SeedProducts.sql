-- Seed initial products into database
-- Run this after the API is stopped and database is updated

INSERT INTO Products (Id, Name, Carrier, PerOccurrence, Aggregate, MinAnnualRevenue, MaxAnnualRevenue, NaicsAllowed, CreatedAt)
VALUES 
('prod-001', 'General Liability - SME', 'Acme Insurance', 1000000, 2000000, 0, 5000000, '445310,722511', GETUTCDATE()),
('prod-002', 'Property - Retail', 'Acme Insurance', 500000, 1000000, 0, 2000000, '445110,445120', GETUTCDATE()),
('prod-003', 'Workers Comp - SME', 'Beta Mutual', 1000000, 1000000, 0, 3000000, '722511,561720', GETUTCDATE());