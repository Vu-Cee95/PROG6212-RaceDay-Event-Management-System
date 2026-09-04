-- =============================================
-- RaceDay Event Management System - Database Script
-- Matches ERD exactly
-- =============================================

CREATE DATABASE RaceDay;
GO
USE RaceDay;
GO

-- 1. Create USERS table
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    ProfilePictureUrl NVARCHAR(500) NULL,
    DateRegistered DATETIME DEFAULT GETDATE()
);
GO

-- 2. Create EVENTTYPES table
CREATE TABLE EventTypes (
    EventTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) UNIQUE NOT NULL
);
GO

-- 3. Create EVENTS table
CREATE TABLE Events (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    EventTypeId INT NOT NULL FOREIGN KEY REFERENCES EventTypes(EventTypeId),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    EventDate DATETIME NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Distance DECIMAL(5,2) NOT NULL,
    BannerImageUrl NVARCHAR(500) NULL,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- 4. Create CATEGORIES table
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL FOREIGN KEY REFERENCES Events(EventId),
    CategoryName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(100) NULL
);
GO

-- 5. Create ENROLMENTS table
CREATE TABLE Enrolments (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    EventId INT NOT NULL FOREIGN KEY REFERENCES Events(EventId),
    CategoryId INT NOT NULL FOREIGN KEY REFERENCES Categories(CategoryId),
    EnrolmentDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
);
GO

-- 6. Create RESULTS table
CREATE TABLE Results (
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Enrolments(EnrolmentId),
    FinishTime TIME(0) NULL,
    FinishingPosition INT NULL,
    Notes NVARCHAR(255) NULL
);
GO

-- =============================================
-- Seed Data (Realistic Sample Data)
-- =============================================

-- Event Types
INSERT INTO EventTypes (TypeName) VALUES ('Run'), ('Walk'), ('Cycle');

-- Users (2 Organisers, 2 Participants)
INSERT INTO Users (Email, PasswordHash, FullName, Role) VALUES 
('thabo.mokoena@raceday.co.za', 'Hash123!', 'Thabo Mokoena', 'Organiser'),
('sarah.vandermerwe@raceday.co.za', 'Hash123!', 'Sarah van der Merwe', 'Organiser'),
('lwando.dlamini@gmail.com', 'Hash123!', 'Lwando Dlamini', 'Participant'),
('priya.naidoo@gmail.com', 'Hash123!', 'Priya Naidoo', 'Participant');

-- Events (3 Events)
INSERT INTO Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, Distance) VALUES 
(1, 1, 'Soweto Marathon', 'The ultimate road running challenge in Johannesburg.', '2026-11-01 06:00:00', 'Soweto, Johannesburg', 42.20),
(1, 3, 'Cape Town Cycle Tour', 'The worlds largest timed cycle race.', '2026-03-08 06:15:00', 'Cape Town, Western Cape', 109.00),
(2, 2, 'Durban Park Run', 'A community 5km walk and run.', '2026-04-18 07:00:00', 'Durban, KwaZulu-Natal', 5.00);

-- Categories (For each event)
INSERT INTO Categories (EventId, CategoryName, Description) VALUES 
(1, 'Under 20', 'Junior runners under the age of 20'),
(1, 'Senior', 'Open category for all ages over 20'),
(2, 'Elite', 'Professional and experienced cyclists'),
(2, 'Amateur', 'Recreational cyclists'),
(3, 'Walkers', 'Participants walking the 5km route'),
(3, 'Runners', 'Participants running the 5km route');

-- Enrolments (Sample)
INSERT INTO Enrolments (ParticipantId, EventId, CategoryId, Status) VALUES 
(3, 1, 2, 'Confirmed'),
(4, 2, 4, 'Pending'),
(3, 3, 5, 'Confirmed'),
(4, 1, 1, 'Confirmed');

-- Results (Sample - For the confirmed enrolments)
INSERT INTO Results (EnrolmentId, FinishTime, FinishingPosition, Notes) VALUES 
(1, '03:45:22', 120, 'Personal Best'),
(3, '00:28:45', 45, NULL);
GO

-- Show seeded data
SELECT * FROM Users;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;
