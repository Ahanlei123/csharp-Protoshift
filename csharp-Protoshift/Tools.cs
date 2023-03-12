﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XC.RSAUtil;

namespace csharp_Protoshift
{
    internal static class Tools
    {
        public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

        static Random ran = new Random();

        /// <summary>
        /// Generate a random string with length of [len]. 
        /// </summary>
        public static string RandomStr(int len)
        {
            string charset = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
            string res = "";
            while (len-- > 0)
            {
                res += charset[ran.Next(0, 61)];
            }
            return res;
        }

        public async static Task ExecuteProcess(string path, string args)
        {
            var p = Process.Start(path, args);
            await p.WaitForExitAsync();
        }

        public static JsonElement GetFieldFromJson(string json, string fieldName)
        {
            var doc = JsonDocument.Parse(json);
#pragma warning disable CS8603 // 可能返回 null 引用。
            return doc.RootElement.GetProperty(fieldName);
#pragma warning restore CS8603 // 可能返回 null 引用。
        }

        /// <summary>
        /// Load the rsa key as <see cref="RSAUtilBase"/>.
        /// </summary>
        /// <param name="rsaKey">The string rsa key, support public/private, PKCS1/PKCS8/Xml all.</param>
        /// <param name="keySize">The bits key size, e.g. 2048-bit</param>
        /// <returns></returns>
        public static RSAUtilBase LoadRSAKey(string rsaKey, int keySize = 2048)
        {
            // PKCS8 Padding
            if (rsaKey.StartsWith("-----BEGIN PUBLIC KEY-----"))
                return new RsaPkcs8Util(publicKey: rsaKey, keySize: keySize);
            else if (rsaKey.StartsWith("-----BEGIN PRIVATE KEY-----"))
                return new RsaPkcs8Util(privateKey: rsaKey, keySize: keySize);
            // PKCS1 Padding
            else if (rsaKey.StartsWith("-----BEGIN RSA PUBLIC KEY-----"))
                return new RsaPkcs1Util(publicKey: rsaKey, keySize: keySize);
            else if (rsaKey.StartsWith("-----BEGIN RSA PRIVATE KEY-----"))
                return new RsaPkcs1Util(privateKey: rsaKey, keySize: keySize);
            // .NET XML Format
            else if (rsaKey.StartsWith("<RSAKeyValue>"))
            {
                if (rsaKey.Contains("<InverseQ>"))
                    return new RsaXmlUtil(privateKey: rsaKey, keySize: keySize);
                else return new RsaXmlUtil(publicKey: rsaKey, keySize: keySize);
            }
            else throw new ArgumentException("Invalid RSA Key!", nameof(rsaKey));
        }

        #region ChatGPT Show Time
        // Code in this region are all Generated by ChatGPT.

        /// <summary>
        /// Compress <paramref name="files"/> into <paramref name="zipFilePath"/>. 
        /// </summary>
        public static void CompressFiles(string zipFilePath, IEnumerable<string> files)
        {
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    archive.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }
        } 

        public static void CompressFiles(string zipFilePath, params string[] files)
            => CompressFiles(zipFilePath, files);

        /// <summary>
        /// Can be applied to both file and directory. Generate suffix like (1), (2) for the <paramref name="path"/> when the file/directory already exists.
        /// </summary>
        public static string AddNumberedSuffixToPath(string filePath)
        {
            /* 该方法首先检查给定路径是否已存在。
             * 如果是文件路径，则将文件名分离为文件名和扩展名，并在文件名后添加一个括号附加编号，直到找到可用的文件名。
             * 如果是目录路径，则附加数字后缀到目录名直到找到可用的目录名。
             * 例如，如果传入的参数是"C:\Users\Example\Desktop\test.txt"，
             * 如果该路径已经存在，则返回"C:\Users\Example\Desktop\test (1).txt"。 
             * 
             * 如果参数是"C:\Users\Example\Desktop\test"，
             * 如果该路径已经存在，则返回"C:\Users\Example\Desktop\test (1)"。 
             * 如果路径不存在，则返回原始路径。
             */
            if (File.Exists(filePath))
            {
                string directory = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                string newFilePath = filePath;
                int suffix = 1;

                while (File.Exists(newFilePath))
                {
                    newFilePath = Path.Combine(directory, string.Format("{0} ({1}){2}", fileName, suffix, extension));
                    suffix++;
                }

                return newFilePath;
            }
            else if (Directory.Exists(filePath))
            {
                string directoryName = Path.GetDirectoryName(filePath);
                string directory = Path.Combine(directoryName, Path.GetFileName(filePath));
                string newDirectory = directory;
                int suffix = 1;

                while (Directory.Exists(newDirectory))
                {
                    newDirectory = Path.Combine(directoryName, string.Format("{0} ({1})", Path.GetFileName(filePath), suffix));
                    suffix++;
                }

                return newDirectory;
            }
            else
            {
                return filePath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static bool TryRemoveColorInfo(string input, out string output)
        {
            try
            {
                int startIndex = 0;
                output = string.Empty;

                while (true)
                {
                    int colorStart = input.IndexOf("<color=", startIndex); //查找下一个彩色文字的起始位置
                    if (colorStart == -1) //若未找到，输出剩余部分并退出循环
                    {
                        output = input.Substring(startIndex, input.Length - startIndex);
                        return true;
                    }

                    int colorEnd = input.IndexOf(">", colorStart); //查找彩色文字的结束位置
                    if (colorEnd == -1) //若未找到，输出剩余部分并退出循环
                    {
                        output = input.Substring(startIndex, input.Length - startIndex);
                        return true;
                    }

                    string colorCode = input.Substring(colorStart + 7, colorEnd - colorStart - 7); //提取颜色代码
                    ConsoleColor color;

                    if (Enum.TryParse(colorCode, out color)) //尝试将字符串颜色代码解析为ConsoleColor枚举类型
                    {
                        output += input.Substring(startIndex, colorStart - startIndex)); //输出彩色文字前的部分
                        int textStart = colorEnd + 1;
                        int textEnd = input.IndexOf("</color>", textStart); //查找彩色文字结束标记
                        if (textEnd == -1) //若未找到，输出剩余部分并退出循环
                        {
                            output += input.Substring(textStart, input.Length - textStart);
                            return true;
                        }
                        output += input.Substring(textStart, textEnd - textStart); //输出彩色文字
                        startIndex = textEnd + 8; //继续查找下一个彩色文字的起始位置
                    }
                    else //解析失败，跳过此次查找
                    {
                        startIndex = colorEnd + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                output = ex.ToString();
                return false;
            }
        }
        #endregion
    }
}
