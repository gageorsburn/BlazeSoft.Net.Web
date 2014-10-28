namespace BlazeSoft.Security.Cryptography
{
    internal class Encryption
    {
        public static string EncryptString(string original)
        {
            string encrypted = string.Empty;

            for (int character = 0; character < original.Length; character++)
                encrypted += original[character] ^ 0x56;

            return encrypted;
        }

        public static string DecryptString(string encrypted)
        {
            string decrypted = string.Empty;

            for (int character = 0; character < encrypted.Length; character++)
                decrypted += encrypted[character] ^ 0x56;
            
            return decrypted;
        }
    }
}