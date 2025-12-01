using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataAccess.Validators
{
    public static class UserValidator
    {
        public const int PASSWORD_MIN_LENGTH = 10;
        public const int PASSWORD_MAX_LENGTH = 16;
        private static readonly Regex _gmailRegex =
            new Regex(@"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@gmail\.com$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private static readonly Regex _outlookRegex =
            new Regex(@"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@outlook\.com$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private static readonly Regex _uvEstudiantesMxRegex =
            new Regex(@"^zS\d{8}@estudiantes\.uv\.mx$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        public static bool ValidateEmailFormat(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }
            return (_gmailRegex.IsMatch(email) || _outlookRegex.IsMatch(email) || _uvEstudiantesMxRegex.IsMatch(email));
        }

        public static bool ValidatePassword(string password)
        {
            List<bool> passwordRequirements = new List<bool>();
            passwordRequirements.Add(MeetsMinLength(password));
            passwordRequirements.Add(WithinMaxLength(password));
            passwordRequirements.Add(HasUpper(password));
            passwordRequirements.Add(HasLower(password));
            passwordRequirements.Add(HasDigit(password));
            passwordRequirements.Add(HasSpecial(password));
            
            return !passwordRequirements.Contains(false);
        }

        private static bool MeetsMinLength(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Length >= PASSWORD_MIN_LENGTH;
        }

        private static bool WithinMaxLength(string password)
        {
            return password != null && password.Length <= PASSWORD_MAX_LENGTH;
        }

        private static bool HasUpper(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);
        }

        private static bool HasLower(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Any(char.IsLower);
        }

        private static bool HasDigit(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);
        }

        private static bool HasSpecial(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
}
