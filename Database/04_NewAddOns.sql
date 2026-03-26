CREATE TABLE Tickets_AddOns (
    ticket_id INT NOT NULL FOREIGN KEY REFERENCES Tickets(ticket_id),
    addon_id INT NOT NULL FOREIGN KEY REFERENCES AddOns(addon_id),
    PRIMARY KEY (ticket_id, addon_id)
);

CREATE TABLE Memberships_AddOns_Discounts (
    membership_id INT NOT NULL FOREIGN KEY REFERENCES Memberships(membership_id),
    addon_id INT NOT NULL FOREIGN KEY REFERENCES AddOns(addon_id),
    discount_percentage TINYINT NOT NULL DEFAULT 0 CHECK (discount_percentage <= 100),
    PRIMARY KEY (membership_id, addon_id)
);
