CREATE TABLE Memberships (
    membership_id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(50) NOT NULL,
    flight_discount_percentage TINYINT NOT NULL DEFAULT 0 CHECK (flight_discount_percentage <= 100)
);

CREATE TABLE Users (
    user_id INT PRIMARY KEY IDENTITY(1,1),
    email NVARCHAR(100) NOT NULL UNIQUE,
    phone NVARCHAR(20),
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    membership_id INT FOREIGN KEY REFERENCES Memberships(membership_id)
);
