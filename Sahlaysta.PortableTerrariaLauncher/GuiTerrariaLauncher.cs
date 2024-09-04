using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Sahlaysta.PortableTerrariaLauncher
{

    /// <summary>
    /// Launches Terraria process.
    /// </summary>
    internal static class GuiTerrariaLauncher
    {

        public delegate void StartedCallback();

        public delegate void EndedCallback();

        public delegate void ErrorCallback(Exception exception);

        private static readonly object activityLock = new object();

        private static readonly object runningProcessLock = new object();
        private static Process runningProcess;

        public static void LaunchTerraria(
            string exeFile,
            string saveDir,
            bool commandLine,
            StartedCallback startedCallback,
            EndedCallback endedCallback,
            ErrorCallback errorCallback)
        {
            new Thread(() =>
            {
                Exception exception = null;
                try
                {
                    if (!Monitor.TryEnter(activityLock))
                    {
                        throw new Exception("LaunchTerraria() is already active");
                    }
                    try
                    {
                        string fullExeFile = Path.GetFullPath(exeFile);
                        string exeContainedDir = Directory.GetParent(fullExeFile).FullName;
                        string fullSaveDir = saveDir == null ? null : Path.GetFullPath(saveDir);

                        using (Process process = new Process())
                        {
                            if (!commandLine)
                            {
                                process.StartInfo.FileName = fullExeFile;
                                if (fullSaveDir != null)
                                {
                                    process.StartInfo.Arguments = "-savedirectory \"" + fullSaveDir + "\"";
                                }
                                process.StartInfo.WorkingDirectory = exeContainedDir;
                            }
                            else
                            {
                                process.StartInfo.FileName = "cmd.exe";
                                string subcmd = "^\"" + CmdEscapeCaretQuoteNestedWin32Path(fullExeFile) + "^\"" +
                                    (fullSaveDir == null ? "" : " -savedirectory ^\"" +
                                        CmdEscapeCaretQuoteNestedWin32Path(fullSaveDir) + "^\"");
                                process.StartInfo.Arguments = "/c " +
                                "\"" +
                                    "title Run & " +
                                    "echo Run command & " +
                                    "echo " + subcmd + " & " +
                                    "echo Press any key to confirm . . . & " +
                                    "pause>Nul & " +
                                    "echo Running command & " +
                                    "echo. & " +
                                    "( " +
                                    "( " +
                                    "start /wait /b ^\"^\" " + subcmd + " ) " +
                                    ") & " +
                                    "echo. & " +
                                    "call echo Exit status %^errorlevel% & " +
                                    "echo Press any key to close this window . . . & " +
                                    "pause>Nul " +
                                "\"";
                                process.StartInfo.WorkingDirectory = exeContainedDir;
                            }

                            process.Start();
                            try
                            {
                                lock (runningProcessLock)
                                {
                                    runningProcess = process;
                                }
                            }
                            finally
                            {
                                try
                                {
                                    startedCallback();
                                }
                                catch (Exception e)
                                {
                                    Console.Error.WriteLine(e);
                                }
                                finally
                                {
                                    try
                                    {
                                        process.WaitForExit();
                                    }
                                    finally
                                    {
                                        try
                                        {
                                            endedCallback();
                                        }
                                        catch (Exception e)
                                        {
                                            Console.Error.WriteLine(e);
                                        }
                                        finally
                                        {
                                            lock (runningProcessLock)
                                            {
                                                runningProcess = null;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(activityLock);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    if (exception != null)
                    {
                        if (errorCallback == null)
                        {
                            Console.Error.WriteLine(exception);
                        }
                        else
                        {
                            try
                            {
                                errorCallback(exception);
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                            }
                        }
                    }
                }
            }).Start();
        }

        private static string CmdEscapeCaretQuoteNestedWin32Path(string path)
        {
            return path
                .Replace("^", "^^")
                .Replace("%", "^%")
                .Replace(" ", "^ ")
                .Replace("&", "^&")
                .Replace("(", "^(")
                .Replace(")", "^)");
        }

        public static bool IsActive()
        {
            if (!Monitor.TryEnter(activityLock))
            {
                return true;
            }
            try
            {
                return false;
            }
            finally
            {
                Monitor.Exit(activityLock);
            }
        }

        public static void KillStartedProcess()
        {
            lock (runningProcessLock)
            {
                try
                {
                    runningProcess?.Kill();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

    }
}