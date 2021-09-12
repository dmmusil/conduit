if not exists(select *
              from conduit.INFORMATION_SCHEMA.TABLES
              where TABLE_NAME = 'Articles')
    begin
        create table dbo.Articles
        (
            ArticleId      varchar(32)   not null,
            Title          varchar(250)  not null,
            TitleSlug      varchar(300)  not null,
            Description    varchar(1000) not null,
            Body           varchar(max)  not null,
            AuthorId       varchar(32)   not null,
            AuthorUsername varchar(50)   not null,
            AuthorBio      varchar(200)  null,
            AuthorImage    varchar(200)  null,
            PublishDate    datetime2     not null,
            UpdatedDate    datetime2     null,
            FavoriteCount  int           not null default 0,
            constraint PK_Articles primary key (ArticleId),
            constraint UIX_TitleSlug unique (TitleSlug)
        )
    end