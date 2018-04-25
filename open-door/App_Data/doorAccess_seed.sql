use [doorAccess];
GO
INSERT INTO [ProfileTypes] (name) VALUES
('Gerente QA'),
('Gerente Desarrollo'),
('Desarrollador'),
('Tester'),
('HR Staffing'),
('Financial Staffing');
GO
INSERT INTO [Users] (email, token, is_active, name, last_name, signup_date, profileType_id) VALUES
('aldo.matus@nearshoretechnology.com',NULL,1,'Aldo Rafael', 'Matus Angulo',NULL,3),
('alfredo.carbajal@nearshoretechnology.com',NULL,1,'José Alfredo', 'Carbajal Santillan',NULL,3),
('fernando.rivera@nearshoretechnology.com',NULL,1,'Fernando', 'Rivera Vazquez',NULL,3),
('javier.alvarado@nearshoretechnology.com',NULL,1,'Javier', 'Alvarado Gonzalez',NULL,2);
GO