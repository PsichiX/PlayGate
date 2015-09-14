using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlayGate
{
    public static class Utils
    {
        #region Public enumerators.

        public enum SymLinkFlag
        {
            File = 0,
            Directory = 1
        }

        #endregion


        #region Public nested classes.

        public class EscapedString
        {
            static readonly Dictionary<string, string> m_replaceDict = new Dictionary<string, string>();

            const string ms_regexEscapes = @"[\a\b\f\n\r\t\v\\""]";

            public static string Escape(string s)
            {
                return Regex.Replace(s, ms_regexEscapes, MatchEscape);
            }

            public static string Unescape(string s)
            {
                return Regex.Unescape(s);
            }

            private static string MatchEscape(Match m)
            {
                string match = m.ToString();
                if (m_replaceDict.ContainsKey(match))
                    return m_replaceDict[match];
                throw new NotSupportedException();
            }

            static EscapedString()
            {
                m_replaceDict.Add("\a", @"\a");
                m_replaceDict.Add("\b", @"\b");
                m_replaceDict.Add("\f", @"\f");
                m_replaceDict.Add("\n", @"\n");
                m_replaceDict.Add("\r", @"\r");
                m_replaceDict.Add("\t", @"\t");
                m_replaceDict.Add("\v", @"\v");
                m_replaceDict.Add("\\", @"\\");
                m_replaceDict.Add("\0", @"\0");
                m_replaceDict.Add("\"", "\\\"");
            }
        }

        #endregion



        #region Public properties.

        public static bool IsNodeJsExists
        {
            get
            {
                string result = RunShell("where", "node");
                return !String.IsNullOrEmpty(result) && File.Exists(result.Trim());
            }
        }

        #endregion



        #region Public static functionality.

        public static string GetRelativePath(string path, string relativeTo)
        {
            Uri from = new Uri(path);
            Uri to = new Uri(relativeTo);
            Uri result = to.MakeRelativeUri(from);
            return Uri.UnescapeDataString(result.ToString());
        }

        public static string RunShell(string app, string args)
        {
            try
            {
                Process proc = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = app;
                info.Arguments = args;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardOutput = true;
                proc.StartInfo = info;
                proc.Start();
                proc.WaitForExit();
                return proc.StandardOutput.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return null;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymLinkFlag dwFlags);

        public static bool IsSymbolicLink(string path)
        {
            if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                return info.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo info = new DirectoryInfo(path);
                return info.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
            else
                return false;
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool SynchronizeDirectories(string sourcePath, string targetPath)
        {
            DirectoryInfo sourceInfo = new DirectoryInfo(sourcePath);
            DirectoryInfo targetInfo = new DirectoryInfo(targetPath);
            if (!sourceInfo.Exists || !targetInfo.Exists)
                return false;
            FileInfo[] sourceArray = sourceInfo.GetFiles("*", SearchOption.AllDirectories);
            FileInfo[] targetArray = targetInfo.GetFiles("*", SearchOption.AllDirectories);
            List<FileInfo> sourceList = sourceArray.ToList();
            List<FileInfo> targetList = targetArray.ToList();
            for (int i = targetList.Count - 1; i >= 0; --i)
            {
                FileInfo info = targetList[i];
                FileInfo found = sourceList.FirstOrDefault(fi => Utils.GetRelativePath(fi.FullName, sourcePath + Path.DirectorySeparatorChar) == Utils.GetRelativePath(info.FullName, targetPath + Path.DirectorySeparatorChar));
                if (found != null)
                {
                    info.Delete();
                    targetList.RemoveAt(i);
                }
            }
            for (int i = 0; i < sourceList.Count; ++i)
            {
                FileInfo info = sourceList[i];
                FileInfo found = targetList.FirstOrDefault(fi => Utils.GetRelativePath(fi.FullName, targetPath + Path.DirectorySeparatorChar) == Utils.GetRelativePath(info.FullName, sourcePath + Path.DirectorySeparatorChar));
                if (found == null || found.Length != info.Length || found.LastWriteTime != info.LastWriteTime)
                {
                    string path = Path.Combine(targetPath, Utils.GetRelativePath(info.FullName, sourcePath + Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    info.CopyTo(path);
                }
            }
            return true;
        }

        #endregion
    }
}
