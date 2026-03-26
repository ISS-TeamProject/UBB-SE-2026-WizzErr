CREATE TABLE Tickets (
ticket_id INT PRIMARY KEY IDENTITY(1,1),
user_id INT NOT NULL FOREIGN KEY REFERENCES Users(user_id),
flight_id INT NOT NULL FOREIGN KEY REFERENCES Flights(id),
seat NVARCHAR(10),
price DECIMAL(10, 2) NOT NULL,
status NVARCHAR(20) DEFAULT 'Active',
passenger_first_name NVARCHAR(50) NOT NULL,
passenger_last_name NVARCHAR(50) NOT NULL,
passenger_email NVARCHAR(100),
passenger_phone NVARCHAR(20)
);
