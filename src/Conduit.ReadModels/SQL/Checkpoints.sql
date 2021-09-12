if not exists(select *
              from conduit.INFORMATION_SCHEMA.TABLES
              where TABLE_NAME = 'Checkpoints')
    begin
        create table dbo.Checkpoints
        (
            Id       varchar(200) not null,
            Position bigint       null,
            constraint PK_Checkpoints primary key (Id)
        )
    end