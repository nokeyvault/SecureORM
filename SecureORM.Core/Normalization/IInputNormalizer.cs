namespace SecureORM.Core.Normalization;

/// <summary>
/// Normalizes input strings before OPE encoding. Implement this interface
/// to handle Unicode, accented characters, or custom transformations.
/// Note: Decoding returns the normalized form, not the original input.
/// </summary>
public interface IInputNormalizer
{
    string Normalize(string input);
}
