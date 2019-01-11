using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LiveUEPM.Tests
{
    using App;

    internal class Program
    {
        public const string ProjectPathGroup = "ProjectPath";

        private static string _openendProjectpath;
        private static bool fOpenException;

        public static string CurrentOpenedProjectPath
        {
            get
            {
                if (fOpenException)
                    return string.Empty;

                try
                {
                    if (string.IsNullOrEmpty(_openendProjectpath))
                    {
                        string contents = GetFileText(EditorLogPath);
                        _openendProjectpath = new Regex($@"COMMAND LINE ARGUMENTS:.+?\n.+?\n(?<{ProjectPathGroup}>(.+?)\nUsing)").Matches(contents).Cast<Match>().First().Groups[ProjectPathGroup].Value;
                        // $@",.+?,\+(?<{ProjectPathGroup}>(.+?/Assets))"""
                    }

                    return _openendProjectpath;
                }
                catch
                {
                    fOpenException = true;
                    return string.Empty;
                }
            }
        }

        public static string ProyectName
        {
            get
            {
                return Path.GetFileName(CurrentOpenedProjectPath);
            }
        }

        public static string EditorLogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");
        private static ProcessWatch Watcher { get; }

        static Program()
        {
            Watcher = new ProcessWatch(true);
            Watcher.UnityInitialized += (p) =>
            {
                Console.WriteLine($"Project '{ProyectName}' was opened!");
            };
            Watcher.UnityInitialized += (p) =>
            {
                Console.WriteLine($"Project '{ProyectName}' was closed!");
            };
        }

        private static void Main(string[] args)
        {
            Console.WriteLine(EditorLogPath);

            //using (var watcher = new ProcessWatch())
            Console.Read();

            Watcher?.Dispose();
        }

        private static string GetFileText(string path)
        {
            string text;

            using (FileStream logFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var logFileReader = new StreamReader(logFileStream))
                text = logFileReader.ReadToEnd();

            return text;
        }
    }
}