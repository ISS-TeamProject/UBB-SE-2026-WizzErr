CREATE TABLE Companies(
    id int IDENTITY(1,1) PRIMARY KEY,
    name varchar(20)
)

CREATE TABLE Airports(
    id int IDENTITY(1,1) PRIMARY KEY,
    city varchar(30),
    name VARCHAR(100),
    code VARCHAR(10)
)

CREATE TABLE Gates(
    id int IDENTITY(1,1) PRIMARY KEY,
    name varchar(20)
)

CREATE TABLE Routes(
    id int IDENTITY(1,1) PRIMARY KEY,
    company_id int FOREIGN KEY REFERENCES Companies(id),
    route_type varchar(10),
    airport_id int FOREIGN KEY REFERENCES Airports(id),
    reccurence_interval int,
    start_date date,
    end_date date,
    departure_time datetime,
    arrival_time datetime,
    capacity int
)

CREATE TABLE Flights(
    id int IDENTITY(1,1) PRIMARY KEY,
    route_id int FOREIGN KEY REFERENCES Routes(id),
    date datetime,
    runway_id int,
    gate_id int FOREIGN KEY REFERENCES Gates(id),
    flight_number varchar(50)
)

INSERT INTO Companies(name) VALUES
('WizzAir'),
('Lufthansa');
GO

INSERT INTO Airports(city, name, code) VALUES
('London', 'London Luton Airport', 'LTN'),
('Munich', 'Munich Airport', 'MUC'),
('Cluj-Napoca', 'Cluj International Airport', 'CLJ');
GO

INSERT INTO Gates(name) VALUES
('Gate 1'),
('Gate 2'),
('Gate 3'),
('Gate 4');
GO

INSERT INTO Routes(
    company_id,
    route_type,
    airport_id,
    reccurence_interval,
    start_date,
    end_date,
    departure_time,
    arrival_time,
    capacity
)
VALUES
(1, 'DEP', 1, 1, '2026-03-01', '2026-12-31', '1900-01-01 08:30:00', '1900-01-01 10:45:00', 180),
(1, 'ARR', 1, 1, '2026-03-01', '2026-12-31', '1900-01-01 11:30:00', '1900-01-01 13:40:00', 180),
(2, 'DEP', 2, 2, '2026-03-01', '2026-12-31', '1900-01-01 14:00:00', '1900-01-01 15:20:00', 160),
(2, 'ARR', 2, 2, '2026-03-01', '2026-12-31', '1900-01-01 16:00:00', '1900-01-01 17:25:00', 160);
GO

INSERT INTO Flights(route_id, date, runway_id, gate_id, flight_number) VALUES
(1, '2026-03-27 08:30:00', 1, 1, 'W6 3401'),
(2, '2026-03-27 13:40:00', 2, 2, 'W6 3402'),
(3, '2026-03-27 14:00:00', 3, 3, 'LH 1671'),
(4, '2026-03-27 17:25:00', 1, 4, 'LH 1672'),
(1, '2026-03-28 08:30:00', 2, 1, 'W6 3401'),
(2, '2026-03-28 13:40:00', 3, 2, 'W6 3402'),
(1, '2026-03-29 08:30:00', 1, 2, 'W6 3401'),
(2, '2026-03-29 13:40:00', 2, 3, 'W6 3402'),
(3, '2026-03-29 14:00:00', 1, 4, 'LH 1671'),
(4, '2026-03-29 17:25:00', 2, 1, 'LH 1672'),
(1, '2026-03-30 08:30:00', 3, 1, 'W6 3401'),
(2, '2026-03-30 13:40:00', 1, 2, 'W6 3402'),
(1, '2026-03-31 08:30:00', 2, 3, 'W6 3401'),
(2, '2026-03-31 13:40:00', 3, 4, 'W6 3402'),
(3, '2026-03-31 14:00:00', 1, 1, 'LH 1671'),
(4, '2026-03-31 17:25:00', 2, 2, 'LH 1672'),
(1, '2026-04-01 08:30:00', 1, 1, 'W6 3401'),
(2, '2026-04-01 13:40:00', 2, 2, 'W6 3402');
GO
