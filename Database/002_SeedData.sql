-- =============================================
-- Script: Seed Data - Test Payments
-- Description: Insert test data for development
-- =============================================

USE [risk-evalutation];
GO

  INSERT INTO PaymentStatus (Id, Name) VALUES
      (1, 'evaluating'),
      (2, 'accepted'),
      (3, 'denied');

    

  INSERT INTO PaymentMethods (Id, Name) VALUES
      (1, 'debit'),
      (2, 'credit');

PRINT 'Seed data inserted successfully';
GO
