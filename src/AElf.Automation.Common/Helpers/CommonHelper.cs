﻿using System;
using System.IO;
using System.Text;

namespace AElf.Automation.Common.Helpers
{
    public static class CommonHelper
    {
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
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
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
    }
}