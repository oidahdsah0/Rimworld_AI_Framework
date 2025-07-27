namespace RimAI.Framework.Configuration.Models
{
    public class MergedConfig
    {
        public ProviderTemplate Template { get; }
        public UserConfig User { get; }

        public MergedConfig(ProviderTemplate template, UserConfig user)
        {
            Template = template;
            User = user;
        }
    }
}