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
