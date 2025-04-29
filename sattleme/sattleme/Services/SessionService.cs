using System.Threading.Tasks;
using Xamarin.Essentials;

namespace sattleme.Services
{
    public class SessionService
    {
        private const string UserUidKey = "user_uid";

        public async Task SaveUserSessionAsync(string userUid)
        {
            await SecureStorage.SetAsync(UserUidKey, userUid);
        }

        public async Task<string> GetUserSessionAsync()
        {
            return await SecureStorage.GetAsync(UserUidKey);
        }

        public void ClearUserSession()
        {
            SecureStorage.Remove(UserUidKey);
        }
    }
}
