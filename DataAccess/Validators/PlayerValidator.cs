
namespace DataAccess.Validators
{
    public static class PlayerValidator
    {
        /// <summary>
        /// Verifies if a Player's profile follows can be inserted on the database
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True if the player's profile is valid, otherwise false</returns>
        public static bool ValidatePlayerProfile(Player player)
        {
            if (player == null || player.User == null)
            {
                return false;
            }
            if (!ValidateIdentificationData(player))
            {
                return false;
            }
            if (!ValidatePersonalData(player))
            {
                return false;
            }
            if (!ValidateSocialMediaData(player))
            {
                return false;
            }
            return true;
        }

        private static bool ValidateIdentificationData(Player player)
        {
            const int MAX_USERNAME_LENGTH = 20;
            const int MAX_EMAIL_LENGTH = 30;

            if (string.IsNullOrEmpty(player.User.email) || player.User.email.Length > MAX_EMAIL_LENGTH)
            {
                return false;
            }
            if (string.IsNullOrEmpty(player.username) || player.username.Length > MAX_USERNAME_LENGTH)
            {
                return false;
            }
            return true;
        }

        private static bool ValidatePersonalData(Player player)
        {
            const int MAX_NAME_LENGTH = 20;
            const int MAX_LASTNAME_LENGTH = 30;
            if (!string.IsNullOrEmpty(player.name) && player.name.Length > MAX_NAME_LENGTH)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(player.lastName) && player.lastName.Length > MAX_LASTNAME_LENGTH)
            {
                return false;
            }
            return true;
        }

        private static bool ValidateSocialMediaData(Player player)
        {
            const int SOCIAL_MEDIA_LENGTH = 30;
            if (!string.IsNullOrEmpty(player.facebookUsername) && player.facebookUsername.Length > SOCIAL_MEDIA_LENGTH)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(player.instagramUsername) && player.instagramUsername.Length > SOCIAL_MEDIA_LENGTH)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(player.discordUsername) && player.discordUsername.Length > SOCIAL_MEDIA_LENGTH)
            {
                return false;
            }
            return true;
        }
    }
}
