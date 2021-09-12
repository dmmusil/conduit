if not exists(select *
              from conduit.INFORMATION_SCHEMA.TABLES
              where TABLE_NAME = 'Favorites')
    begin
        create table dbo.Favorites
        (
            ArticleId varchar(32) not null,
            UserId    varchar(32) not null,
            constraint PK_Favorites primary key (ArticleId, UserId),
        )
    end