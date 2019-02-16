namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public class JwtInfoProvider
    {
        public JwtInfoProvider(string key)
        {
            Key = key;
        }
        public string Key { get;  }
    }
}
