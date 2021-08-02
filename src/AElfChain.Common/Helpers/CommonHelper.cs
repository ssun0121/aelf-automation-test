using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AElf;
using AElf.Cryptography;
using Org.BouncyCastle.Crypto.Digests;

namespace AElfChain.Common.Helpers
{
    public static class CommonHelper
    {
        public static readonly string AppRoot = AppDomain.CurrentDomain.BaseDirectory;

        public static string ApplicationName =>
            Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName;

        public static string GetDefaultDataDir()
        {
            try
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "aelf");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetCurrentDataDir()
        {
            try
            {
                var path = Path.Combine(AppRoot, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                var contractPath = Path.Combine(path, "contracts");
                if (!Directory.Exists(contractPath))
                    Directory.CreateDirectory(contractPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void CopyFiles(string originPath, string desPath)
        {
            if (!File.Exists(originPath)) throw new FileNotFoundException();

            if (!Directory.Exists(desPath))
            {
                Directory.CreateDirectory(desPath);
                if (!Directory.Exists(desPath)) throw new DirectoryNotFoundException();
            }

            var fileName = Path.GetFileName(originPath);
            File.Copy(originPath, Path.Combine(desPath, fileName), true);
        }

        public static bool DeleteDirectoryFiles(string path)
        {
            if (!Directory.Exists(path)) return false;

            Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            return true;
        }

        public static string RandomString(int size, bool lowerCase)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var builder = new StringBuilder(size);
            var startChar = lowerCase ? 97 : 65; //65 = A / 97 = a
            for (var i = 0; i < size; i++)
                builder.Append((char) (26 * random.NextDouble() + startChar));
            return builder.ToString();
        }

        public static byte[] GenerateRandombytes(long length)
        {
            var bytes = new byte[length];
            var rand = new Random(Guid.NewGuid().GetHashCode());
            rand.NextBytes(bytes);

            return bytes;
        }

        public static int GenerateRandomNumber(int min, int max)
        {
            var rd = new Random(Guid.NewGuid().GetHashCode());
            var random = rd.Next(min, max);
            return random;
        }

        public static string ConvertMileSeconds(long elapsedMilliseconds)
        {
            var minutes = elapsedMilliseconds / 60000;
            var seconds = elapsedMilliseconds % 60000 / 1000;
            var milliseconds = elapsedMilliseconds % 1000;

            var stamp = string.Empty;

            if (minutes != 0)
                stamp += $"{minutes: 00}m:";
            if (minutes != 0 || seconds != 0)
                stamp += $"{seconds: 00}s:";
            stamp += $"{milliseconds: 000}ms";

            return stamp;
        }

        public static string MapPath(string virtualPath)
        {
            return AppRoot + virtualPath.TrimStart('~');
        }

        public static void ConsoleChangeLine()
        {
            var cursorPosition = Console.CursorLeft;
            if (cursorPosition != 0)
                Console.WriteLine();
        }
        
        public static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T) v.GetValue(new Random().Next(v.Length));
        }

        public static List<int> TakeRandomNumberList(int count, int min, int max, bool isDuplicate = false)
        {
            var list = new List<int>();

                for (int i = 0; i < count; i++)
                {
                    var number = GenerateRandomNumber(min, max);
                    if (!isDuplicate)
                    {
                        while (list.Contains(number))
                            number = GenerateRandomNumber(min, max);
                    }
                    list.Add(number);
                }
   
            return list;
        }
        
        public static string GenerateAddressOnEthereum(string publicKey)
        {
            if (publicKey.StartsWith("0x"))
            {
                publicKey = publicKey.Substring(2, publicKey.Length - 2);
            }

            publicKey = publicKey.Substring(2, publicKey.Length - 2);
            publicKey = GetKeccak256(publicKey);
            var address = "0x" + publicKey.Substring(publicKey.Length - 40, 40);
            return address;
        }

        private static string GetKeccak256(string hexMsg)
        {
            var offset = hexMsg.StartsWith("0x") ? 2 : 0;

            var txByte = Enumerable.Range(offset, hexMsg.Length - offset)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexMsg.Substring(x, 2), 16))
                .ToArray();

            //Note: Not intended for intensive use so we create a new Digest.
            //if digest reuse, prevent concurrent access + call Reset before BlockUpdate
            var digest = new KeccakDigest(256);

            digest.BlockUpdate(txByte, 0, txByte.Length);
            var calculatedHash = new byte[digest.GetByteLength()];
            digest.DoFinal(calculatedHash, 0);

            var transactionHash = BitConverter.ToString(calculatedHash, 0, 32).Replace("-", "").ToLower();

            return transactionHash;
        }

        public static Signature GenerateSignature(string hexMsg, byte[] privateKey)
        {
            var msgHashBytes = ByteStringHelper.FromHexString(GetKeccak256(hexMsg));
            var recoverableInfo = CryptoHelper.SignWithPrivateKey(privateKey, msgHashBytes.ToByteArray());
            var rBytes = recoverableInfo.Take(32).ToArray();
            var sBytes = recoverableInfo.Skip(32).Take(32).ToArray();
            var vBytes = recoverableInfo.Skip(64).Take(1).ToArray();
            return new Signature
            {
                HashMsg = msgHashBytes.ToHex(),
                RecoverInfo = recoverableInfo.ToHex(),
                R = rBytes.ToHex(),
                S = sBytes.ToHex(),
                V = vBytes.ToHex()
            };
        }

        public class Signature
        {
            public string HashMsg { get; set; }
            public string RecoverInfo { get; set; }
            public string R { get; set; }
            public string S { get; set; }
            public string V { get; set; }
        }

    }
}