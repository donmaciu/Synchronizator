using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Chomik;
using System.Text.RegularExpressions;

namespace ChomikSynchro.Chomik
{
    class Synchronizer
    {
        

        public Synchronizer()
        {
            Settings.LoadSettings("paths.set");
        }

        public void Synchronize()
        {
            foreach(PathVersion UpdateSet in Settings.Path)
            {
                try
                {

                    if (File.Exists(UpdateSet.VersionPath))
                    {
                        string version = GetVersion(UpdateSet.VersionPath);

                        if(PathVersion.IsGreaterVersion(UpdateSet.Version, version))
                        {
                            GetFiles(UpdateSet.Path, UpdateSet.OutputPaths);
                            UpdateSet.UpdateVersion(version);
                        }

                    }
                    else
                        throw new Exception("Nie znaleziono pliku version.txt w folderze " + UpdateSet.Path);

                } catch (Exception e)
                {
                    Logger.Log(e.Message);
                } finally
                {
                    Settings.SaveSettings();
                }
            }
        }
        
        string GetVersion(string versionPath)
        {
            string result = "0";
            using(StreamReader file = new StreamReader(versionPath))
            {
                string buffer = "";

                while(file.Peek() > -1)
                {
                    buffer = file.ReadLine();
                    if(buffer != "")
                    {
                        result = buffer;
                    }
                }
            }
            return result;
        }

        void GetFiles(string inputDir , string[] outputDir)
        {
            if (!Directory.Exists("Temp"))
                Directory.CreateDirectory("Temp");
            if (!Directory.Exists("Backup"))
                Directory.CreateDirectory("Backup");

            EmptyDir("Temp");

            CopyAllFiles(inputDir, "Temp");
            
            foreach(string output in outputDir)
            {
                if (Directory.Exists(output))
                {
                    string backupFolder = Path.Combine(output, "Backup");
                    EmptyDir("Backup");

                    MoveAllFiles(output, "Backup");
                    EmptyDir(output);
                    if(!Directory.Exists(backupFolder))
                        Directory.CreateDirectory(backupFolder);

                    MoveAllFiles("Backup", backupFolder);

                    CopyAllFiles("Temp", output);
                }
            }


        }

        void EmptyDir(string targetdir)
        {
            DirectoryInfo di = new DirectoryInfo(targetdir);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        void MoveAllFiles(string source , string target)
        {
            DirectoryInfo di = new DirectoryInfo(source);

            foreach (FileInfo file in di.GetFiles())
            {
                file.MoveTo(Path.Combine(target , file.Name));
            }
            
        }

        void CopyAllFiles(string source, string target)
        {
            DirectoryInfo di = new DirectoryInfo(source);

            foreach (FileInfo file in di.GetFiles())
            {
                file.CopyTo(Path.Combine(target, file.Name));
            }
        }
    }

    class Settings
    {
        public static List<PathVersion> Path = new List<PathVersion>();
        static string path = "paths.set";

        /// <summary>
        /// Wczytuje ustawienia synchronizacji z pliku, którego ścieżka ustawiona
        /// jest w zmiennej statycznej path obiektu Settings.
        /// Wczytane ustawienia lądują w liście Path.
        /// </summary>
        /// <param name="settingsPath"></param>
        public static void LoadSettings(string settingsPath)
        {
            path = settingsPath;

            try
            {
                using(StreamReader file = new StreamReader(path))
                {
                    string buffer = "";
                    string innerPath = "";
                    string innerVersion = "";
                    string OutputPathsContainer = "";
                    List<string> OutputPaths = new List<string>();

                    //Format linii: "\\ścieżka\do\folderu\z\ddlkami"{{wersja}}>>"folder wyjsciowy""folder wyjsciowy"<<
                    while (file.Peek() > -1)
                    {
                        buffer = file.ReadLine();
                        innerPath = "";
                        innerVersion = "";

                        // Regex: \"([^\"]*)\"
                        innerPath = Regex.Match(buffer, "\\\"([^\\\"]*)\\\"").Groups[1].Value;

                        // Regex: {{([^\{]*)}}
                        innerVersion = Regex.Match(buffer, @"{{([^\{]*)}}").Groups[1].Value;

                        // Regex: >>([^]*)<<
                        OutputPathsContainer = Regex.Match(buffer, @">>([^{]*)<<").Groups[1].Value;

                        foreach(Match match in Regex.Matches(OutputPathsContainer, "\\\"([^\\\"]*)\\\""))
                        {
                            if(match.Groups[1].Value != "")
                                OutputPaths.Add(match.Groups[1].Value);
                        }

                        if (innerPath != "" && innerVersion != "")
                            Path.Add(new PathVersion(innerPath, innerVersion, OutputPaths.ToArray()));

                    }
                }
            } catch (Exception e)
            {
                Logger.Log(e.Message);
            }


        }

        public static void SaveSettings()
        {
            try
            {
                using(StreamWriter file = new StreamWriter(path))
                {
                    foreach (PathVersion ver in Path)
                    {
                        file.WriteLine(ver.ToString());
                    }
                }
                
            } catch (Exception e)
            {
                Logger.Log(e.Message);
            }
        }
    }

    class PathVersion
    {
        public string Path { get; private set; }
        public string Version { get; private set; }
        public string[] OutputPaths { get; private set; }

        public string VersionPath
        {
            get
            {
                return System.IO.Path.Combine(Path, "version.txt");
            }
        }
        

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="Path">Ściężka aktualizacji</param>
        /// <param name="Version">Wersja ostatniej aktualizacji</param>
        /// <param name="OutputPaths">Lista ścieżek do zaktualizowania</param>
        public PathVersion(string Path , string Version, string[] OutputPaths)
        {
            this.Path = Path;
            this.Version = Version;
            this.OutputPaths = OutputPaths;
        }

        /// <summary>
        /// Porównuje wersje, i jeśli wersja z aktualizacji jest wyższa od wersji z urządzenia
        /// zwraca wartość TRUE.
        /// 
        /// W przeciwnym wypadku zwraca FALSE
        /// </summary>
        /// <param name="Machine">Wersja obecna na maszynie</param>
        /// <param name="Update">Wersja aktualizacji</param>
        /// <returns></returns>
        public static bool IsGreaterVersion(string Machine, string Update)
        {
            string[] MachineVer = Machine.Split('.');
            string[] UpdateVer = Update.Split('.');
            int MachineInt = 0;
            int UpdateInt = 0;

            //Wyliczenie najmniejszej liczby segmentów z wersji
            int VerSegments = (MachineVer.Length >= UpdateVer.Length ? UpdateVer.Length : MachineVer.Length);

            for (int i = 0; i < VerSegments; i++)
            {
                MachineInt = 0;
                UpdateInt = 0;
                int.TryParse(MachineVer[i], out MachineInt);
                int.TryParse(UpdateVer[i], out UpdateInt);

                if (UpdateInt > MachineInt)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Zmienia wersje dla danej ścieżki
        /// </summary>
        /// <param name="Version">Wersja podmieniająca w zmiennych starą</param>
        public void UpdateVersion(string Version)
        {
            this.Version = Version;
        }

        /// <summary>
        /// Zwraca obiekt w formacie do zapisu w pliku ustawień
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            string result = "";

            result += $"\"{Path}\"";
            result += "{{" + Version + "}}";
            result += ">>";

            foreach(string path in OutputPaths)
            {
                result += $"\"{path}\"";
            }

            result += "<<";

            return result;
        }
    }
}
