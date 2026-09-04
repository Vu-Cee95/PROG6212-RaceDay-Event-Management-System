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
