if not exists(select *
              from conduit.INFORMATION_SCHEMA.TABLES
              where TABLE_NAME = 'Followers')
    begin
        create table dbo.Followers
        (
            FollowedUserId  varchar(32) not null,
            FollowingUserId varchar(32) not null,
            constraint PK_Followers primary key (FollowedUserId, FollowingUserId)
        )
    end