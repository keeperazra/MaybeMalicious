using System;
using System.Collections.Generic;
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
                PrintHelp();
                return;
            }

            Dictionary<string, string> kwargs = ProcessArgs(args);

            string fileName = kwargs["target"];
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

        static Dictionary<string, string> ProcessArgs(string[] args)
        {
            Dictionary<string, string> kwargs = new Dictionary<string, string>
            {
                ["action"] = "decrypt"
            };
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string value = null;
                if (arg.Contains('='))
                {
                    value = arg.Split('=')[1];
                    arg = arg.Split('=')[0];
                }
                else if (i + 1 < args.Length && args[i + 1][0] != '-')
                {
                    // The next index of args exists and is not a parameter, therefore it should be treated as a value
                    i++;
                    value = args[i];
                }
                switch (arg)
                {
                    case "-f":
                        kwargs["target"] = value ?? throw new ArgumentException("Detected -f, but no file name was supplied!");
                        kwargs["type"] = "file";
                        break;
                    case "-d":
                        kwargs["target"] = value ?? throw new ArgumentException("Detected -d, but no directory path was supplied!");
                        kwargs["type"] = "directory";
                        break;
                    case "--key":
                        kwargs["key"] = value ?? throw new ArgumentException("Detected --key, but no key was provided!");
                        break;
                    case "--keyfile":
                        kwargs["key"] = FetchKeyFromFile(value);
                        break;
                    case "-e":
                        kwargs["action"] = "encrypt";
                        break;
                    default:
                        Console.WriteLine($"Ignoring unknown parameter \"{arg}\"");
                        break;
                }
            }
            return kwargs;
        }

        static void PrintHelp()
        {
            Console.WriteLine("Expected syntax: MaybeMalicious [-f | -d] <path> [--key=<key> | --keyfile=<key_file>]");
            Console.WriteLine();
            Console.WriteLine("--key       to supply the private key to decrypt your files.");
            Console.WriteLine("--keyfile   to supply the path to a file that contains the private key.");
            Console.WriteLine("-f          to decrypt the specified file using the provided key.");
            Console.WriteLine("-d          to decrypt all the files in the specified directory.");
            Console.WriteLine();
            Console.WriteLine("Secret options: -e");
            Console.WriteLine("These will encrypt your files!");
        }

        static string FetchKeyFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Cannot find key file at " + filePath);
            }

            using FileStream file = new FileStream(filePath, FileMode.Open);
            using StreamReader read = new StreamReader(file);

            return read.ReadToEnd();
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
