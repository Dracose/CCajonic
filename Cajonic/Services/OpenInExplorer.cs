using System.IO;

namespace Cajonic.Services
{
    public static class OpenInExplorer
    {
        public static void OpenInOsxFileExplorer(string path)
        {
            bool openInsidesOfFolder = false;
 
            string macPath = path.Replace("\\", "/");
 
            if (Directory.Exists(macPath))
            {
                openInsidesOfFolder = true;
            }

            if (!macPath.StartsWith("\""))
            {
                macPath = "\"" + macPath;
            }
            if (!macPath.EndsWith("\""))
            {
                macPath += "\"";
            }
            string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;
            try
            {
                System.Diagnostics.Process.Start("open", arguments);
            }
            
            catch(System.ComponentModel.Win32Exception e)
            {
                e.HelpLink = "";
            }
        }
        
        public static void OpenInWindowsFileExplorer(string path)
        { 
            bool openInsidesOfFolder = false;

            string winPath = path.Replace("/", "\\");
            if (Directory.Exists(winPath))
            { 
                openInsidesOfFolder = true;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + winPath);
            }
            catch(System.ComponentModel.Win32Exception e) 
            {
                e.HelpLink = ""; 
            }
        }
    }
}