using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Passerelle.Host.Validation;

public sealed class EmojiAttribute() : ValidationAttribute("Validation_Emoji_Invalid")
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string s || string.IsNullOrEmpty(s))
            return ValidationResult.Success;

        var hasPrimaryEmoji = false;

        foreach (int codePoint in s.EnumerateRunes().Select(rune => rune.Value))
        {
            if (IsPrimary(codePoint))
            {
                hasPrimaryEmoji = true;
                continue;
            }

            if (IsModifier(codePoint))
                continue;

            return new ValidationResult(ErrorMessageString);
        }

        return hasPrimaryEmoji
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessageString);
    }

    private static bool IsPrimary(int cp) =>
        (cp is >= 0x1F300 and <= 0x1F5FF) || // Misc Symbols & Pictographs
        (cp is >= 0x1F600 and <= 0x1F64F) || // Emoticons
        (cp is >= 0x1F680 and <= 0x1F6FF) || // Transport & Map
        (cp is >= 0x1F900 and <= 0x1F9FF) || // Supplemental Symbols & Pictographs
        (cp is >= 0x1FA70 and <= 0x1FAFF) || // Symbols & Pictographs Extended-A
        (cp is >= 0x2600 and <= 0x26FF) || // Misc Symbols
        (cp is >= 0x2700 and <= 0x27BF) || // Dingbats
        (cp is >= 0x1F1E6 and <= 0x1F1FF); // Regional Indicators (flags)

    private static bool IsModifier(int cp) =>
        cp == 0xFE0F || // Variation Selector-16
        cp == 0x200D || // Zero Width Joiner
        (cp is >= 0x1F3FB and <= 0x1F3FF); // Skin tone modifiers
}
