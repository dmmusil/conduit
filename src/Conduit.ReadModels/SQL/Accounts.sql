if not exists(select *
              from conduit.INFORMATION_SCHEMA.TABLES
              where TABLE_NAME = 'Accounts')
    begin
        create table dbo.Accounts
        (
            StreamId     varchar(32)   not null,
            Email        varchar(200)  not null,
            Username     varchar(50)   not null,
            PasswordHash varchar(200)  not null,
            Bio          varchar(1000) null,
            Image        varchar(200)  null,
            constraint PK_Accounts primary key (StreamId)
        )
    end