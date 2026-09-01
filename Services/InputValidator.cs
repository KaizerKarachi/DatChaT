using FamilyChat.Constants;

namespace FamilyChat.Services;

public static class InputValidator
{
    public static (bool ok, string error) ValidateNickname(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick))
            return (false, "Ник не может быть пустым");
        if (nick.Length > AppConstants.MaxNicknameLength)
            return (false, "Ник слишком длинный (макс. 30 символов)");
        if (nick.Any(char.IsControl))
            return (false, "Ник содержит недопустимые символы");
        return (true, "");
    }

    public static (bool ok, string error) ValidatePassword(string pass)
    {
        if (string.IsNullOrWhiteSpace(pass))
            return (false, "Пароль не может быть пустым");
        if (pass.Length < AppConstants.MinPasswordLength)
            return (false, "Пароль слишком короткий (мин. 6 символов)");
        if (pass.Length > AppConstants.MaxPasswordLength)
            return (false, "Пароль слишком длинный");
        return (true, "");
    }

    public static (bool ok, string error) ValidateMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (false, "Сообщение пустое");
        if (text.Length > AppConstants.MaxMessageLength)
            return (false, "Сообщение слишком длинное (макс. 4000 символов)");
        return (true, "");
    }
}