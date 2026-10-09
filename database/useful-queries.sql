-- Open this file in SSMS after the API has started once (it creates the TicketDeskDb database automatically).
-- Connect to:  (localdb)\MSSQLLocalDB   (or the server you put in appsettings.json)
USE TicketDeskDb;
GO

-- 1) Everything at a glance
SELECT t.Id, t.Title, t.Status, t.Priority, t.Category, t.Sentiment, u.FullName AS Customer, a.FullName AS Agent, t.CreatedAt
FROM Tickets t
JOIN Users u ON u.Id = t.CreatedById
LEFT JOIN Users a ON a.Id = t.AssignedToId
ORDER BY t.CreatedAt DESC;

-- 2) Tickets by category and priority
SELECT Category, Priority, COUNT(*) AS Total
FROM Tickets GROUP BY Category, Priority ORDER BY Category, Total DESC;

-- 3) Average resolution time (hours) per category
SELECT Category, AVG(DATEDIFF(MINUTE, CreatedAt, ResolvedAt) / 60.0) AS AvgHours
FROM Tickets WHERE ResolvedAt IS NOT NULL GROUP BY Category;

-- 4) Agent workload
SELECT a.FullName, COUNT(*) AS ActiveTickets
FROM Tickets t JOIN Users a ON a.Id = t.AssignedToId
WHERE t.Status IN ('Open', 'InProgress') GROUP BY a.FullName;
GO

-- 5) Example reporting stored procedure (run once if you want to show it in interviews)
CREATE OR ALTER PROCEDURE dbo.usp_TicketSummaryByStatus
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Status, COUNT(*) AS Total FROM dbo.Tickets GROUP BY Status;
END
GO
-- EXEC dbo.usp_TicketSummaryByStatus;
