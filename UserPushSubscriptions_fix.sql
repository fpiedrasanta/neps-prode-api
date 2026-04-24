CREATE TABLE `UserPushSubscriptions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) NOT NULL,
    `Endpoint` longtext NOT NULL,
    `P256dh` longtext NOT NULL,
    `Auth` longtext NOT NULL,
    `UserAgent` longtext NOT NULL,
    `CreatedAt` datetime NOT NULL,
    `LastUsedAt` datetime NULL,
    CONSTRAINT `PK_UserPushSubscriptions` PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX `IX_UserPushSubscriptions_UserId` ON `UserPushSubscriptions` (`UserId`);