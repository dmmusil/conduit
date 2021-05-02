using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    public static class AccountsRegistration
    {
        public static void Register()
        {
            TypeMap.AddType<UserRegistered>(nameof(UserRegistered));
            TypeMap.AddType<UsernameUpdated>(nameof(UsernameUpdated));
            TypeMap.AddType<PasswordUpdated>(nameof(PasswordUpdated));
            TypeMap.AddType<BioUpdated>(nameof(BioUpdated));
            TypeMap.AddType<ImageUpdated>(nameof(ImageUpdated));
            TypeMap.AddType<EmailUpdated>(nameof(EmailUpdated));
            TypeMap.AddType<AccountFollowed>(nameof(AccountFollowed));
            TypeMap.AddType<AccountUnfollowed>(nameof(AccountUnfollowed));
        }
    }
}