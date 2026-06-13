USE ScientificTrendDB;
GO

IF COL_LENGTH('dbo.Users', 'ActorType') IS NULL
    ALTER TABLE dbo.Users
    ADD ActorType NVARCHAR(50) NOT NULL
        CONSTRAINT DF_Users_ActorType DEFAULT N'Student';
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.Roles WHERE RoleName = N'Administrator'
)
BEGIN
    INSERT INTO dbo.Roles (RoleName)
    VALUES (N'Administrator');
END
GO

DECLARE @Email NVARCHAR(255) = N'admin@gmail.com';
DECLARE @PasswordHash NVARCHAR(255) = N'lEhJbjHxbGrtihPBbdI2ew==.9NT0Zep8ACnWarNTtewD9PpVRbEFhw7tWHNZwlBUs0c=';
DECLARE @AdminRoleId INT;
DECLARE @UserId INT;

SELECT @AdminRoleId = RoleId
FROM dbo.Roles
WHERE RoleName = N'Administrator';

SELECT @UserId = UserId
FROM dbo.Users
WHERE Email = @Email;

IF @UserId IS NULL
BEGIN
    INSERT INTO dbo.Users (
        Email,
        PasswordHash,
        FullName,
        DateOfBirth,
        PhoneNumber,
        ActorType,
        CreatedAt,
        IsActive
    )
    VALUES (
        @Email,
        @PasswordHash,
        N'System Administrator',
        '2000-01-01',
        N'0900000000',
        N'SystemAdministrator',
        SYSDATETIME(),
        1
    );

    SET @UserId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Users
    SET PasswordHash = @PasswordHash,
        FullName = N'System Administrator',
        ActorType = N'SystemAdministrator',
        IsActive = 1
    WHERE UserId = @UserId;
END

IF NOT EXISTS (
    SELECT 1
    FROM dbo.UserRoles
    WHERE UserId = @UserId
      AND RoleId = @AdminRoleId
)
BEGIN
    INSERT INTO dbo.UserRoles (UserId, RoleId)
    VALUES (@UserId, @AdminRoleId);
END

SELECT u.UserId, u.Email, u.FullName, u.ActorType, u.IsActive, r.RoleName
FROM dbo.Users u
JOIN dbo.UserRoles ur ON ur.UserId = u.UserId
JOIN dbo.Roles r ON r.RoleId = ur.RoleId
WHERE u.UserId = @UserId;



admin@gmail.com
admin123