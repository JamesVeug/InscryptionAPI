namespace InscryptionAPI.Helpers;

public static class GeneralHelpers
{
    public static string ConvertToRomanNumeral(int number)
    {
        if (number < 1 || number > 3999)
        {
            throw new ArgumentOutOfRangeException("number", "Number must be between 1 and 3999.");
        }
    
        // Define arrays to hold the Roman numeral symbols and their corresponding values.
        string[] symbols = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
    
        // Initialize an empty string to hold the Roman numeral.
        string result = "";
    
        // Loop through the arrays, subtracting the values until the number is reduced to zero.
        for (int i = 0; i < values.Length; i++)
        {
            while (number >= values[i])
            {
                result += symbols[i];
                number -= values[i];
            }
        }
    
        return result;
    }
}
