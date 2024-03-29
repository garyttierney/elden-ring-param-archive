﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SoulsFormats;

/* https://github.com/JKAnderson/Yapped */
namespace SoulsParamsConverter
{
    public class RegulationFile
    {
        private static byte[] erRegulationKey = SFUtil.ParseHexString(
            "99 bf fc 36 6a 6b c8 c6 f5 82 7d 09 36 02 d6 76 c4 28 92 a0 1c 20 7f b0 24 d3 af 4e 49 3f ef 99");
        private static byte[] ds3RegulationKey = Encoding.ASCII.GetBytes("ds3#jn/8_7(rsY9pg55GFN7VFL#+3n/)");

        public static BND4 Load(FileInfo path)
        {
            return path.Extension switch
            {
                ".bdt" => RegulationFile.DecryptDS3Regulation(path.FullName),
                ".bin" => RegulationFile.DecryptERRegulation(path.FullName),
                ".dcx" => BND4.Read(path.FullName),
                _ => throw new InvalidOperationException($"{path.Name} is not a valid parambnd or regulation file")
            };
        }
        
        /// <summary>
        /// Decrypts and unpacks DS3's regulation BND4 from the specified path.
        /// </summary>
        public static BND4 DecryptDS3Regulation(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bytes = DecryptByteArray(ds3RegulationKey, bytes);
            return BND4.Read(bytes);
        }        
        
        /// <summary>
        /// Decrypts and unpacks DS3's regulation BND4 from the specified path.
        /// </summary>
        public static BND4 DecryptERRegulation(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bytes = DecryptByteArray(erRegulationKey, bytes);
            return BND4.Read(DCX.Decompress(bytes));
        }

        /// <summary>
        /// Repacks and encrypts DS3's regulation BND4 to the specified path.
        /// </summary>
        public static void EncryptDS3Regulation(string path, BND4 bnd)
        {
            byte[] bytes = bnd.Write();
            bytes = EncryptByteArray(ds3RegulationKey, bytes);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }

        private static byte[] EncryptByteArray(byte[] key, byte[] secret)
        {
            using (MemoryStream ms = new MemoryStream())
            using (AesManaged cryptor = new AesManaged())
            {
                cryptor.Mode = CipherMode.CBC;
                cryptor.Padding = PaddingMode.PKCS7;
                cryptor.KeySize = 256;
                cryptor.BlockSize = 128;

                byte[] iv = cryptor.IV;

                using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                {
                    cs.Write(secret, 0, secret.Length);
                }

                byte[] encryptedContent = ms.ToArray();

                byte[] result = new byte[iv.Length + encryptedContent.Length];

                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);

                return result;
            }
        }

        private static byte[] DecryptByteArray(byte[] key, byte[] secret)
        {
            byte[] iv = new byte[16];
            byte[] encryptedContent = new byte[secret.Length - 16];

            Buffer.BlockCopy(secret, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(secret, iv.Length, encryptedContent, 0, encryptedContent.Length);

            using (MemoryStream ms = new MemoryStream())
            using (AesManaged cryptor = new AesManaged())
            {
                cryptor.Mode = CipherMode.CBC;
                cryptor.Padding = PaddingMode.None;
                cryptor.KeySize = 256;
                cryptor.BlockSize = 128;

                using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateDecryptor(key, iv), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedContent, 0, encryptedContent.Length);
                }

                return ms.ToArray();
            }
        }
    }
}