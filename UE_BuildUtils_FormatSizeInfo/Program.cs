using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UE_BuildUtils_FormatSizeInfo
{
    enum ExitCode : int
    {
        Success = 0,
        Error = 1
    }

    public enum BuildOS : int
    {
        None = 0,
        Android = 1,
        iOS = 2
    }

    // Wrapper around the directory class
    public static class Directory_Wrapper
    {
        // Same as Directory.EnumerateFiles but expects to find no more than one file, handles exceptions.
        public static String EnumerateFiles_OneOrNone(String searchPath, String searchPattern, SearchOption searchOption)
        {
            try
            {
                var pathlist = Directory.EnumerateFiles(searchPath, searchPattern, searchOption);

                if (pathlist.Count() > 1)
                {
                    Console.WriteLine("EnumerateFiles_OneOrNone Error: Multiple files found in % for %.", searchPath, searchPattern);
                    Environment.Exit((int)ExitCode.Error);
                }
                else if (pathlist.Count() == 1)
                {
                    return pathlist.First();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit((int)ExitCode.Error);
            }

            return null;
        }

        // Same as Directory.EnumerateFiles but expects to find exactly one file, handles exceptions.
        public static String EnumerateFiles_OneExactly(String searchPath, String searchPattern, SearchOption searchOption)
        {
            try
            {
                var pathlist = Directory.EnumerateFiles(searchPath, searchPattern, searchOption);

                if (pathlist.Count() > 1)
                {
                    Console.WriteLine("EnumerateFiles_OneExactly Error: Multiple files found in {0} for {1}.", searchPath, searchPattern);
                    Environment.Exit((int)ExitCode.Error);
                }
                else if (pathlist.Count() == 0)
                {
                    Console.WriteLine("EnumerateFiles_OneExactly Error: No files found in {0} for {1}.", searchPath, searchPattern);
                    Environment.Exit((int)ExitCode.Error);
                }
                else
                {
                    return pathlist.First();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit((int)ExitCode.Error);
            }
            return null;
        }

        // Same as Directory.EnumerateFiles but expects to find exactly one file, handles exceptions.
        public static String EnumerateDirectories_OneExactly(String searchPath, String searchPattern, SearchOption searchOption)
        {
            try
            {
                var pathlist = Directory.EnumerateDirectories(searchPath, searchPattern, searchOption);

                if (pathlist.Count() > 1)
                {
                    Console.WriteLine("EnumerateDirectories_OneExactly Error: Multiple directories found in {0} for {1}.", searchPath, searchPattern);
                    Environment.Exit((int)ExitCode.Error);
                }
                else if (pathlist.Count() == 0)
                {
                    Console.WriteLine("EnumerateDirectories_OneExactly Error: No directory found in {0} for {1}.", searchPath, searchPattern);
                    Environment.Exit((int)ExitCode.Error);
                }
                else
                {
                    return pathlist.First();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit((int)ExitCode.Error);
            }
            return null;
        }

        public static long DirSize(DirectoryInfo d)
        {
            long Size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                Size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                Size += DirSize(di);
            }
            return (Size);
        }
    }

    public struct PathEntry
    {
        public String path;
        public long size;

        public PathEntry(String path_arg)
        {
            path = path_arg;
            size = 0;
        }
    }

    public struct PathInfo
    {
        public PathEntry buildRoot, apk, ipa, libUE4, arm64, unknown, pak_engine, pak_project;
        public BuildOS buildOS;

        public PathInfo(String path_buildRoot, String projectName)
        {
            buildOS = BuildOS.None;
            buildRoot       = new PathEntry(path_buildRoot);
            apk             = new PathEntry(null);
            ipa             = new PathEntry(null);
            libUE4          = new PathEntry(null);
            arm64           = new PathEntry(null);
            unknown         = new PathEntry(null);
            pak_engine      = new PathEntry(null);
            pak_project     = new PathEntry(null);

            apk.path = Directory_Wrapper.EnumerateFiles_OneOrNone(path_buildRoot, "*.apk", SearchOption.AllDirectories);
            if (apk.path != null)
            {
                buildOS = BuildOS.Android;
                apk.size = (new FileInfo(apk.path)).Length;

                libUE4.path = Directory_Wrapper.EnumerateFiles_OneExactly(path_buildRoot, "libUE4.so", SearchOption.AllDirectories);
                libUE4.size = (new FileInfo(libUE4.path)).Length;
            }
            else
            {
                ipa.path = Directory_Wrapper.EnumerateFiles_OneOrNone(path_buildRoot, "*.ipa", SearchOption.AllDirectories);
                ipa.size = (ipa.path == null) ? 0 : (new FileInfo(ipa.path).Length);

                arm64.path = Directory_Wrapper.EnumerateFiles_OneOrNone(path_buildRoot, "*.arm64", SearchOption.AllDirectories);
                arm64.size = (arm64.path == null) ? 0 : (new FileInfo(arm64.path).Length);

                unknown.path = Directory_Wrapper.EnumerateFiles_OneOrNone(path_buildRoot, "*.unknown", SearchOption.AllDirectories);
                unknown.size = (unknown.path == null) ? 0 : (new FileInfo(unknown.path).Length);

                if (arm64.path != null)
                {
                    ///TO DO: Get the single build type path, assume unkown 
                }

                buildOS = BuildOS.iOS;

            }
            
            if (ipa.path == null && apk.path == null)
            {
                Console.WriteLine("UE_BuildUtils_FormatSizeInfo Error: No package found at {0}.", buildRoot);
                Environment.Exit((int)ExitCode.Error);
            }

            String pakDirectory = Directory_Wrapper.EnumerateDirectories_OneExactly(buildRoot.path, "pak", SearchOption.TopDirectoryOnly);
            pak_engine.path = Directory_Wrapper.EnumerateDirectories_OneExactly(pakDirectory, "*Engine*", SearchOption.TopDirectoryOnly);
            pak_engine.size = Directory_Wrapper.DirSize(new DirectoryInfo(pak_engine.path));

            pak_project.path = Directory_Wrapper.EnumerateDirectories_OneExactly(pakDirectory, ("*" + projectName + "*"), SearchOption.TopDirectoryOnly);
            pak_project.size = Directory_Wrapper.DirSize(new DirectoryInfo(pak_project.path));
            // TO DO: Get the rest of the paths.
        }

        public String GetPathsString()
        {
            return String.Format("buildRoot: {0},\napk: {1},\nipa: {2},\nlibUE4: {3},\narm64: {4},\nunknown: {5},\npak_engine: {6},\npak_project: {7}\n",
                buildRoot.path,
                apk.path,
                ipa.path,
                libUE4.path,
                arm64.path,
                unknown.path,
                pak_engine.path,
                pak_project.path);
        }

        public String GetSizeString()
        {
            return String.Format("buildRoot: {0},\napk: {1},\nipa: {2},\nlibUE4: {3},\narm64: {4},\nunknown: {5},\npak_engine: {6},\npak_project: {7}\n",
                apk.size,
                ipa.size,
                libUE4.size,
                arm64.size,
                unknown.size,
                pak_engine.size,
                pak_project.size);
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            int validArgCount = 1;
            // Validate number of arguments passed - display usage format.
            if (args.Count() != validArgCount)
            {
                Console.WriteLine("UE_BuildUtils_FormatSizeInfo Error: Invalid number of arguments, expected {0} - got {1}.", validArgCount, args.Count());
                Console.WriteLine("UE_BuildUtils_FormatSizeInfo usage:\n\tUE_BuildUtils_FormatSizeInfo <build path>");
                return (int)ExitCode.Error;
            }

            PathInfo pathInfo = new PathInfo(args[0], "Rush");
            Console.WriteLine(pathInfo.GetPathsString());
            Console.WriteLine(pathInfo.GetSizeString());

            return (int)ExitCode.Success;
        }
    }
}