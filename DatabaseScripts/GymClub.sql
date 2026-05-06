CREATE DATABASE Amural;
GO

USE Amural;
GO

CREATE TABLE Members (
    MemberId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    BirthDate DATE NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100) NULL,
    Notes NVARCHAR(200) NULL
);
GO

CREATE TABLE Subscriptions (
    SubscriptionId INT IDENTITY(1,1) PRIMARY KEY,
    SubscriptionNumber INT NOT NULL UNIQUE CHECK (SubscriptionNumber BETWEEN 100 AND 9999),
    MemberId INT NOT NULL,
    PlanName NVARCHAR(80) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Price DECIMAL(10,2) NOT NULL CHECK (Price >= 0),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Subscriptions_Members FOREIGN KEY (MemberId) REFERENCES Members(MemberId),
    CONSTRAINT CK_Subscriptions_Dates CHECK (EndDate > StartDate)
);
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    UserPassword NVARCHAR(50) NOT NULL,
    UserRole NVARCHAR(20) NOT NULL CHECK (UserRole IN ('Administrator', 'Trainer', 'Client')),
    DisplayName NVARCHAR(100) NOT NULL,
    MemberId INT NULL,
    CONSTRAINT FK_Users_Members FOREIGN KEY (MemberId) REFERENCES Members(MemberId)
);
GO

CREATE TABLE Visits (
    VisitId INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT NOT NULL,
    VisitDate DATE NOT NULL,
    RegisteredByUserId INT NOT NULL,
    Comment NVARCHAR(120) NULL,
    CONSTRAINT FK_Visits_Members FOREIGN KEY (MemberId) REFERENCES Members(MemberId),
    CONSTRAINT FK_Visits_Users FOREIGN KEY (RegisteredByUserId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Visits_Member_Date UNIQUE (MemberId, VisitDate)
);
GO

CREATE TRIGGER TR_Visits_Register
ON Visits
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE EXISTS (
            SELECT 1
            FROM Visits v
            WHERE v.MemberId = i.MemberId
              AND v.VisitDate = i.VisitDate
        )
    )
    BEGIN
        RAISERROR(N'Посещение не может быть зарегистрировано дважды в один день.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1
            FROM Subscriptions s
            WHERE s.MemberId = i.MemberId
              AND s.IsActive = 1
              AND i.VisitDate BETWEEN s.StartDate AND s.EndDate
        )
    )
    BEGIN
        RAISERROR(N'Нельзя зарегистрировать посещение без активного абонемента.', 16, 1);
        RETURN;
    END;

    INSERT INTO Visits (MemberId, VisitDate, RegisteredByUserId, Comment)
    SELECT MemberId, VisitDate, RegisteredByUserId, Comment
    FROM inserted;
END;
GO

CREATE PROCEDURE AddNewMemberWithSubscription
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50),
    @BirthDate DATE,
    @Phone NVARCHAR(20),
    @Email NVARCHAR(100),
    @Notes NVARCHAR(200),
    @PlanName NVARCHAR(80),
    @StartDate DATE,
    @EndDate DATE,
    @Price DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MemberId INT;
    DECLARE @SubscriptionNumber INT;

    INSERT INTO Members (FirstName, LastName, BirthDate, Phone, Email, Notes)
    VALUES (@FirstName, @LastName, @BirthDate, @Phone, @Email, @Notes);

    SET @MemberId = SCOPE_IDENTITY();

    ;WITH Numbers AS (
        SELECT 100 AS SubscriptionNumber
        UNION ALL
        SELECT SubscriptionNumber + 1
        FROM Numbers
        WHERE SubscriptionNumber < 9999
    )
    SELECT TOP (1) @SubscriptionNumber = n.SubscriptionNumber
    FROM Numbers n
    WHERE NOT EXISTS (
        SELECT 1
        FROM Subscriptions s
        WHERE s.SubscriptionNumber = n.SubscriptionNumber
    )
    OPTION (MAXRECURSION 10000);

    IF @SubscriptionNumber IS NULL
    BEGIN
        RAISERROR(N'Свободные номера абонементов закончились.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM Subscriptions
        WHERE MemberId = @MemberId
          AND IsActive = 1
          AND @StartDate BETWEEN StartDate AND EndDate
    )
    BEGIN
        RAISERROR(N'У клиента уже есть активный абонемент.', 16, 1);
        RETURN;
    END;

    INSERT INTO Subscriptions (SubscriptionNumber, MemberId, PlanName, StartDate, EndDate, Price, IsActive)
    VALUES (@SubscriptionNumber, @MemberId, @PlanName, @StartDate, @EndDate, @Price, 1);
END;
GO

INSERT INTO Members (FirstName, LastName, BirthDate, Phone, Email, Notes) VALUES
(N'Иван',   N'Петров',   '1996-04-12', N'+7 914 000-10-01', N'ivan.petrov@gym.local',   N'Утренние тренировки'),
(N'Анна',   N'Соколова', '1999-07-03', N'+7 914 000-10-02', N'anna.sokolova@gym.local', N'Предпочитаю йогу'),
(N'Максим', N'Орлов',    '1991-01-25', N'+7 914 000-10-03', N'maksim.orlov@gym.local',  N'Силовые'),
(N'Елена',  N'Морозова', '1988-09-19', N'+7 914 000-10-04', N'elena.morozova@gym.local',N'Бассейн и кардио'),
(N'Кирилл', N'Волков',   '2000-12-07', N'+7 914 000-10-05', N'kirill.volkov@gym.local', N'Ожидает продление');
GO

INSERT INTO Subscriptions (SubscriptionNumber, MemberId, PlanName, StartDate, EndDate, Price, IsActive) VALUES
(100, 1, N'Силовой',       DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), DATEADD(DAY, 10, CAST(GETDATE() AS DATE)), 3200, 1),
(101, 2, N'Йога',          DATEADD(DAY, -12, CAST(GETDATE() AS DATE)), DATEADD(DAY, 18, CAST(GETDATE() AS DATE)), 2900, 1),
(102, 3, N'Полный доступ', DATEADD(DAY,  -5, CAST(GETDATE() AS DATE)), DATEADD(DAY, 25, CAST(GETDATE() AS DATE)), 4100, 1),
(103, 4, N'Бассейн',       DATEADD(DAY, -40, CAST(GETDATE() AS DATE)), DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), 2600, 0);
GO

INSERT INTO Users (Login, UserPassword, UserRole, DisplayName, MemberId) VALUES
(N'admin', N'admin123', N'Administrator', N'Администратор зала', NULL),
(N'coach', N'coach123', N'Trainer',       N'Старший тренер',     NULL),
(N'anna',  N'anna123',  N'Client',        N'Анна Соколова',      2);
GO

INSERT INTO Visits (MemberId, VisitDate, RegisteredByUserId, Comment) VALUES
(1, DATEADD(DAY, -9, CAST(GETDATE() AS DATE)), 2, N'Кардио'),
(2, DATEADD(DAY, -9, CAST(GETDATE() AS DATE)), 2, N'Растяжка'),
(3, DATEADD(DAY, -9, CAST(GETDATE() AS DATE)), 2, N'Ноги'),
(1, DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), 2, N'Спина'),
(2, DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), 2, N'Йога'),
(3, DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), 2, N'Грудь'),
(1, DATEADD(DAY, -7, CAST(GETDATE() AS DATE)), 2, N'Функциональная'),
(2, DATEADD(DAY, -7, CAST(GETDATE() AS DATE)), 2, N'Пилатес'),
(4, DATEADD(DAY, -7, CAST(GETDATE() AS DATE)), 2, N'Последний день'),
(1, DATEADD(DAY, -6, CAST(GETDATE() AS DATE)), 2, N'Интервальная'),
(2, DATEADD(DAY, -6, CAST(GETDATE() AS DATE)), 2, N'Дыхательная практика'),
(3, DATEADD(DAY, -6, CAST(GETDATE() AS DATE)), 2, N'Кроссфит'),
(1, DATEADD(DAY, -5, CAST(GETDATE() AS DATE)), 2, N'Бокс'),
(2, DATEADD(DAY, -5, CAST(GETDATE() AS DATE)), 2, N'Гибкость'),
(3, DATEADD(DAY, -4, CAST(GETDATE() AS DATE)), 2, N'Силовая'),
(1, DATEADD(DAY, -3, CAST(GETDATE() AS DATE)), 2, N'Кор'),
(2, DATEADD(DAY, -3, CAST(GETDATE() AS DATE)), 2, N'Баланс'),
(3, DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), 2, N'Руки'),
(1, DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), 2, N'Спринт'),
(2, CAST(GETDATE() AS DATE),                   2, N'Восстановление');
GO

CREATE VIEW ActiveVisitsLastWeek
AS
SELECT
    v.VisitId,
    v.VisitDate,
    m.LastName + N' ' + m.FirstName AS MemberName,
    s.PlanName
FROM Visits v
INNER JOIN Members m ON m.MemberId = v.MemberId
INNER JOIN Subscriptions s ON s.MemberId = v.MemberId
    AND s.IsActive = 1
    AND v.VisitDate BETWEEN s.StartDate AND s.EndDate
WHERE v.VisitDate BETWEEN DATEADD(DAY, -6, CAST(GETDATE() AS DATE)) AND CAST(GETDATE() AS DATE);
GO

CREATE ROLE GymAdmin;
CREATE ROLE GymTrainer;
CREATE ROLE GymClient;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON Members       TO GymAdmin;
GRANT SELECT, INSERT, DELETE         ON Visits        TO GymAdmin;
GRANT SELECT, INSERT, UPDATE, DELETE ON Subscriptions TO GymAdmin;
GRANT SELECT                         ON Users         TO GymAdmin;
GO

GRANT SELECT, UPDATE         ON Members       TO GymTrainer;
GRANT SELECT, INSERT, DELETE ON Visits        TO GymTrainer;
GRANT SELECT                 ON Subscriptions TO GymTrainer;
GO

GRANT SELECT ON Members       TO GymClient;
GRANT SELECT ON Subscriptions TO GymClient;
GRANT SELECT ON Visits        TO GymClient;
GO
