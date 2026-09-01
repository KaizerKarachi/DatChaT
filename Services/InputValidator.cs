namespace FamilyChat.Services;

public static class InputValidator
{
    public static (bool ok, string error) ValidateNickname(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick))
            return (false, "Ник не может быть пустым");
        if (nick.Length > 30)
            return (false, "Ник слишком длинный (макс. 30 символов)");
        if (nick.Any(c => char.IsControl(c)))
            return (false, "Ник содержит недопустимые символы");
        return (true, "");
    }

    public static (bool ok, string error) ValidatePassword(string pass)
    {
        if (string.IsNullOrWhiteSpace(pass))
            return (false, "Пароль не может быть пустым");
        if (pass.Length < 6)
            return (false, "Пароль слишком короткий (мин. 6 символов)");
        if (pass.Length > 50)
            return (false, "Пароль слишком длинный");
        return (true, "");
    }

    public static (bool ok, string error) ValidateMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (false, "Сообщение пустое");
        if (text.Length > 4000)
            return (false, "Сообщение слишком длинное (макс. 4000 символов)");
        return (true, "");
    }
}
