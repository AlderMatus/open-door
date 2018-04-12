
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 04/12/2018 13:33:39
-- Generated from EDMX file: C:\Users\aldo.matus\Documents\VSProjects\OpenDoor\open-door\open-door\Models\Model1.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;

USE [master]
GO
/****** Object:  Database [doorAccess]    Script Date: 4/12/2018 4:55:29 PM ******/
CREATE DATABASE [doorAccess];
GO
USE [doorAccess];
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_UserAccess]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Accesses] DROP CONSTRAINT [FK_UserAccess];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Accesses]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Accesses];
GO
IF OBJECT_ID(N'[dbo].[Users]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Users];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Users'
CREATE TABLE [dbo].[Users] (
    [Id] int  NOT NULL,
    [email] nvarchar(60)  NOT NULL,
    [token] nchar(20)  NULL,
    [is_active] bit  NOT NULL,
    [name] nvarchar(60)  NOT NULL,
    [last_name] nvarchar(90)  NOT NULL,
    [singup_date] datetime  NULL
);
GO

-- Creating table 'Accesses'
CREATE TABLE [dbo].[Accesses] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [user_id] int  NOT NULL,
    [status] tinyint  NOT NULL,
    [descripcion] nvarchar(150)  NOT NULL,
    [access_date] datetime  NOT NULL,
    [access_time] time  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'Users'
ALTER TABLE [dbo].[Users]
ADD CONSTRAINT [PK_Users]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Accesses'
ALTER TABLE [dbo].[Accesses]
ADD CONSTRAINT [PK_Accesses]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [user_id] in table 'Accesses'
ALTER TABLE [dbo].[Accesses]
ADD CONSTRAINT [FK_UserAccess]
    FOREIGN KEY ([user_id])
    REFERENCES [dbo].[Users]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserAccess'
CREATE INDEX [IX_FK_UserAccess]
ON [dbo].[Accesses]
    ([user_id]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------