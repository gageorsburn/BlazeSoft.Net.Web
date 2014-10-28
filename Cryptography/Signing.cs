using System.Security.Cryptography;

namespace BlazeSoft.Security.Cryptography
{
    internal static class Signing
    {
        internal static byte[] GeneratePrivateKey()
        {
            byte[] PrivateKey;

            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                try
                {
                    PrivateKey = RSA.ExportCspBlob(true);
                }
                catch (CryptographicException)
                {
                    return null;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }

            return PrivateKey;
        }

        internal static byte[] GetPublicKey(byte[] PrivateKey)
        {
            byte[] PublicKey;

            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                try
                {
                    RSA.ImportCspBlob(PrivateKey);
                    PublicKey = RSA.ExportCspBlob(false);
                }
                catch (CryptographicException)
                {
                    return null;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }

            return PublicKey;
        }

        internal static byte[] GenerateSignature(byte[] Data, byte[] KeyBlob)
        {
            byte[] SignedData;

            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                try
                {
                    RSA.ImportCspBlob(KeyBlob);
                    RSAPKCS1SignatureFormatter RSAForm = new RSAPKCS1SignatureFormatter(RSA);
                    RSAForm.SetHashAlgorithm("SHA512");
                    SignedData = RSAForm.CreateSignature(new SHA512Managed().ComputeHash(Data));
                }
                catch (CryptographicException)
                {
                    return null;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }

            return SignedData;
        }

        internal static bool VerifySignature(byte[] Data, byte[] Signature, byte[] KeyBlob)
        {
            bool SignatureValid = false;

            if (Data == null)
                return true;

            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                try
                {
                    RSA.ImportCspBlob(KeyBlob);
                    RSAPKCS1SignatureDeformatter RSADeform = new RSAPKCS1SignatureDeformatter(RSA);
                    RSADeform.SetHashAlgorithm("SHA512");
                    SignatureValid = RSADeform.VerifySignature(new SHA512Managed().ComputeHash(Data), Signature);
                }
                catch (CryptographicException)
                {
                    return false;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }

            return SignatureValid;
        }
    }
}