using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;

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
        
        public static string GetJson<T>(T obj)
        {
            DataContractJsonSerializer json = new DataContractJsonSerializer(obj.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                json.WriteObject(stream,obj);
                string szJson = Encoding.UTF8.GetString(stream.ToArray());
                return szJson;
            }
        }
        
        public static string ConvertJsonString(string str)
        {
            //格式化json字符串
            var serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            var obj = serializer.Deserialize(jtr);
            if (obj == null) return str;
            StringWriter textWriter = new StringWriter();
            JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            };
            serializer.Serialize(jsonWriter, obj);
            return textWriter.ToString();

        }
        
        public static void CreateCVSHeader(string fileName,List<string> headers)
        {
            if (!File.Exists(fileName))
            {
                FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                StringBuilder sb = new StringBuilder();
                foreach (var header in headers)
                {
                    sb.Append(header).Append(",");
                }
                sw.WriteLine(sb);
                sw.Flush();
                sw.Close();
                fs.Close();
            }
        }
    }
}