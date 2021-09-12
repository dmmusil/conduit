if not exists(select *
              from conduit.INFORMATION_SCHEMA.TABLES
              where TABLE_NAME = 'Tags')
    begin
        create table dbo.Tags
        (
            TagId     int          not null identity (1,1),
            ArticleId varchar(32)  not null,
            Tag       varchar(250) not null,
            constraint PK_Tags primary key (TagId),
        )
    end