using System;
using System.IO;
using System.Security.Cryptography;

namespace MaybeMalicious
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No file selected! Exiting!");
                return;
            }

            string fileName = args[0];
            if (!File.Exists(fileName))
            {
                Console.WriteLine(string.Format("File \"{0}\" does not exist!", fileName));
            }
            Console.WriteLine("Selecting file: " + fileName);

            Random r = new Random();
            byte[] key = new byte[32];
            r.NextBytes(key);
            using Aes aes = Aes.Create();
            aes.Key = key;

            EncryptFile(fileName, aes);
            DecryptFile(fileName, aes);
        }

        static void EncryptFile(string filePath, Aes aes)
        {
            string newFilePath = filePath + ".enc";
            if (File.Exists(newFilePath))
            {
                Console.WriteLine(filePath + " is already encrypted. Skipping!");
                return;
            }

            using FileStream file = new FileStream(filePath, FileMode.Open);
            byte[] bytes = GetBytes(file);
            int bytesToWrite = bytes.Length;

            using FileStream newFile = new FileStream(newFilePath, FileMode.Create);
            newFile.Write(aes.IV, 0, aes.IV.Length);
            using CryptoStream crypto = new CryptoStream(newFile, aes.CreateEncryptor(), CryptoStreamMode.Write);
            crypto.Write(bytes, 0, bytesToWrite);

            Console.WriteLine("Encrypted file " + filePath);
        }

        static void DecryptFile(string filePath, Aes aes)
        {
            string encFilePath = filePath + ".enc";
            string decFilePath = filePath + ".dec";
            if (!File.Exists(encFilePath))
            {
                Console.WriteLine("Cannot find encrypted file " + encFilePath);
                return;
            }
            if (File.Exists(decFilePath))
            {
                Console.WriteLine("File has already been decrypted to " + decFilePath);
                return;
            }

            using FileStream encFile = new FileStream(encFilePath, FileMode.Open);
            encFile.Seek(aes.IV.Length, SeekOrigin.Begin);
            using CryptoStream crypto = new CryptoStream(encFile, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using FileStream decFile = new FileStream(decFilePath, FileMode.Create);
            crypto.CopyTo(decFile);
        }

        static byte[] GetBytes(Stream stream)
        {
            byte[] bytes;
            try
            {
                bytes = new byte[stream.Length];
                int bytesToRead = (int)stream.Length;
                int bytesRead = 0;

                while (bytesToRead > 0)
                {
                    int n = stream.Read(bytes, bytesRead, bytesToRead);

                    if (n == 0)
                        break;

                    bytesRead += n;
                    bytesToRead -= n;
                }
            }
            catch
            {
                Console.WriteLine("Stream reading failed!");
                throw;
            }
            return bytes;
        }
    }
}
