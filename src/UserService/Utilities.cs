namespace UserService.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public static class Utilities
{
    public static bool IsValidEmail(string email)
    {
        return new EmailAddressAttribute().IsValid(email);
    }
    public static bool ContainsSpecialCharacters(string input)
    {
        // Sadece harf ve rakamlara izin verir — diğer her şey "özel karakter" sayılır
        return Regex.IsMatch(input, @"[^a-zA-Z0-9]");
    }

    public static bool IsPasswordSixDigit(string password)
    {
        return password.Length > 5;
    }
}