using System;
using System.Linq;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Lighter2Generator
{
    class Program
    {
        static void Main()
        {
            var directory = PromptValue("Where would you like to create this project?\nDirectory must be empty or non-existant.", Directory.GetCurrentDirectory(), ValidateInstallationDirectory);

            var securitySalt = PromptValue("What would you like to use for your security salt?", Guid.NewGuid().ToString(), ValidateSecuritySalt);

            var mysqlHost = PromptValue("What is the MySQL host?", "localhost", ValidateNop);

            var mysqlUsername = PromptValue("What is the MySQL username?", "root", ValidateNop);

            var mysqlPassword = PromptValue("What is the MySQL password?", "", ValidateNop);

            var mysqlDatabase = PromptValue("What is the MySQL database?", "lighter2_app", ValidateNop);

            var assembly = Assembly.GetExecutingAssembly();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ExtractZipFile(assembly.GetManifestResourceStream(assembly.GetManifestResourceNames()[0]), directory);

            WriteTextToFile(Path.Combine(directory, "application\\configs\\security.php"), String.Format(@"<?php
$config = array(
    'type' => 'MD5',
    'salt' =>  {0}
);", EncodePHPString(securitySalt)));

            WriteTextToFile(Path.Combine(directory, "application\\configs\\mysql.php"), String.Format(@"<?php
$config = array(
    'host' => {0},
    'username' => {1},
    'password' => {2},
    'database' => {3}
);", EncodePHPString(mysqlHost), EncodePHPString(mysqlUsername), EncodePHPString(mysqlPassword), EncodePHPString(mysqlDatabase)));
        }

        static void WriteTextToFile(string path, string text)
        {
            var sw = new StreamWriter(path);

            sw.Write(text);

            sw.Close();
        }

        static Tuple<bool, string> ValidateInstallationDirectory(string directory)
        {
            if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            {
                return new Tuple<bool, string>(false, "Directory already exists and is not empty, please select another directory.");
            }

            return new Tuple<bool, string>(true, "");
        }

        static Tuple<bool, string> ValidateSecuritySalt(string securitySalt)
        {
            return securitySalt.Trim() == "" ?
                new Tuple<bool, string>(false, "Salt must not be empty.") :
                new Tuple<bool, string>(true, "");
        }

        static Tuple<bool, string> ValidateNop(string input)
        {
            return new Tuple<bool, string>(true, "");
        }

        static string EncodePHPString(string input)
        {
            var output = input.Replace("\"", "\\\"").Replace("\n", "\\n");

            return "\"" + output + "\"";
        }

        static void ExtractZipFile(Stream stream, string outFolder)
        {
            ZipFile zipFile = null;

            try
            {
                zipFile = new ZipFile(stream);

                foreach (ZipEntry zipEntry in zipFile)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;
                    }

                    var entryFileName = zipEntry.Name;

                    var buffer = new byte[4096];
                    var zipStream = zipFile.GetInputStream(zipEntry);

                    var fullOutFolder = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullOutFolder);

                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    using (var streamWriter = File.Create(fullOutFolder))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
            finally
            {
                if (zipFile != null)
                {
                    zipFile.IsStreamOwner = true;
                    zipFile.Close();
                }
            }
        }

        static string PromptValue(string prompt, string defaultValue, Func<string, Tuple<bool, string>> validator)
        {
            Console.WriteLine(prompt);
            Console.Write(@"(" + defaultValue + @"): ");

            var value = Console.ReadLine();

            if(value == "")
            {
                value = defaultValue;
            }

            var result = validator(value);
            
            if(!result.Item1)
            {
                Console.WriteLine(result.Item2);
                return PromptValue(prompt, defaultValue, validator);
            }

            return value;
        }
    }
}
