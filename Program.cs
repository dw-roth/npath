using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using CommandLine;
using CommandLine.Text;

namespace npath
{
    class Options
    {
        [Option('c', "context", DefaultValue = "user", HelpText = "[user | system]  Specify current user path or system path when adding or deleting path items.")]
        public string Context { get; set; }

        [Option('d', "delete", HelpText = "Delete the specified entry from the path")]
        public string ItemToDelete { get; set; }

        [Option('a', "append", HelpText = "Append item to path.")]
        public string ItemToAppend { get; set; }

        [Option('p', "prepend", HelpText = "Prepend item to path.")]
        public string ItemToPrepend { get; set; }

        [Option('v', "verbose", HelpText = "Verbose mode")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    public class OptionException : Exception
    {
        public OptionException() { }
        public OptionException(string message) : base(message) { }
        public OptionException(string message, Exception inner) : base(message, inner) { }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            try
            {
                options = ParseOptions(args);
                Run(options);
            }
            catch (OptionException e)
            {
                Console.WriteLine("Invalid command line options");
                if (!String.IsNullOrEmpty(e.Message))
                {
                    Console.WriteLine(e.Message);
                }
                options.GetUsage();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }

        static Options ParseOptions(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                throw new OptionException("Invalid argument");
            }

            ThrowOnInvalidOptions(options);
            return options;
        }

        static void Run(Options options)
        {
            if (!String.IsNullOrEmpty(options.ItemToDelete))
            {
                DeletePathItem(options);
            } 
            else if (!String.IsNullOrEmpty(options.ItemToAppend))
            {
                AppendItemToPath(options);
            } 
            else if (!String.IsNullOrEmpty(options.ItemToPrepend))
            {
                PrependItemToPath(options);
            }
            else
            {
                DisplayPath(options);
            }

        }

        static void DisplayPath(Options options)
        {
            IEnumerable<string> path = GetUserPath();
            if (options.Verbose)
            {
                Console.WriteLine("========Current User Path========");
            }
            foreach (var item in path)
            {
                Console.WriteLine(item);
            }

            path = GetSystemPath();
            if (options.Verbose)
            {
                Console.WriteLine("========System Path========");
            }
            foreach (var item in path)
            {
                Console.WriteLine(item);
            }
        }

        static IEnumerable<string> GetUserPath()
        {
            var userEnvKey = Registry.CurrentUser.OpenSubKey("Environment");
            return userEnvKey == null ? null : userEnvKey.GetValue("Path", "").ToString().Split(new[] { ';' });
        }

        static IEnumerable<string> GetSystemPath()
        {
            var sysEnvKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment");
            return sysEnvKey == null ? null : sysEnvKey.GetValue("Path", "").ToString().Split(new[] { ';' });
        }


        static void DeletePathItem(Options options)
        {
            var pathList = options.Context.ToLower().Trim() == "system" ? new List<string>(GetSystemPath()) : new List<string>(GetUserPath());
            var found = false;
            for (var i = 0; i < pathList.Count; i++)
            {
                if (pathList[i].ToLower().Trim() == options.ItemToDelete.ToLower().Trim())
                {
                    pathList.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new OptionException(String.Format("{0} was not found in the path. Did you forget to include '-c system'?", options.ItemToDelete));
            }

            UpdatePath(options, pathList.ToArray());
        }

        static void AppendItemToPath(Options options)
        {
            var pathList = options.Context.ToLower().Trim() == "system" ? new List<string>(GetSystemPath()) : new List<string>(GetUserPath());
            ThrowOnDuplicateEntry(options.ItemToAppend, pathList);
            pathList.Add(options.ItemToAppend);
            UpdatePath(options, pathList.ToArray());
        }

        static void PrependItemToPath(Options options)
        {
            var pathList = options.Context.ToLower().Trim() == "system" ? new List<string>(GetSystemPath()) : new List<string>(GetUserPath());
            ThrowOnDuplicateEntry(options.ItemToPrepend, pathList);
            pathList.Insert(0, options.ItemToPrepend);
            UpdatePath(options, pathList.ToArray());
        }

        static void UpdatePath(Options options, IEnumerable<string> newPath)
        {
            if (options.Context.ToLower().Trim() == "system")
            {
                SetSystemPath(newPath);
            }
            else
            {
                SetUserPath(newPath);
            }
        }

        static void SetUserPath(IEnumerable<string> pathItems)
        {
            var newPath = BuildPathString(pathItems);
            var userEnvKey = Registry.CurrentUser.OpenSubKey("Environment", true);
            if (userEnvKey == null)
            {
                throw new Exception("Unable to open registry key HKEY_CURRENT_USER\\Environemnt");
            }

            userEnvKey.SetValue("Path", newPath, RegistryValueKind.ExpandString);
        }

        static void SetSystemPath(IEnumerable<string> pathItems)
        {
            var newPath = BuildPathString(pathItems);
            var sysEnvKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", true);
            if (sysEnvKey == null)
            {
                throw new Exception("Unable to open registry key SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment");
            }

            sysEnvKey.SetValue("Path", newPath, RegistryValueKind.ExpandString);
        }

        static string BuildPathString(IEnumerable<string> newPath)
        {
            var path = new StringBuilder();
            foreach (var item in newPath)
            {
                path.Append(item + ";");
            }

            path.Remove(path.Length - 1, 1);
            return path.ToString();
        }

        static void ThrowOnInvalidOptions(Options options)
        {
            ThrowOnDeleteAndAdd(options);
            ThrowOnPrependAndAppend(options);
            ThrowOnInvalidContext(options);
        }

        static void ThrowOnDeleteAndAdd(Options options)
        {
            if (!String.IsNullOrEmpty(options.ItemToDelete) && (!String.IsNullOrEmpty(options.ItemToAppend) || !String.IsNullOrEmpty(options.ItemToPrepend)))
            {
                throw new OptionException("Cannot specify and delete and add operation in same command");
            }
        }

        static void ThrowOnPrependAndAppend(Options options)
        {
            if (!String.IsNullOrEmpty(options.ItemToAppend) && !String.IsNullOrEmpty(options.ItemToPrepend))
            {
                throw new OptionException("Cannot specify prepend and append in the same command");
            }
        }

        static void ThrowOnInvalidContext(Options options)
        {
            if (options.Context.ToLower() != "user" && options.Context.ToLower() != "system")
            {
                throw new OptionException("Invalid context");
            }
        }

        static void ThrowOnDuplicateEntry(string entry, IEnumerable<string> pathList)
        {
            if (pathList.Any(item => item.ToLower().Trim() == entry.ToLower().Trim()))
            {
                throw new Exception("item already exists in path");
            }
        }
    }
}
