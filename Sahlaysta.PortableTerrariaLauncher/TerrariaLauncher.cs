using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sahlaysta.PortableTerrariaLauncher
{
    internal class TerrariaLauncher
    {

        public delegate void EndedCallback(Exception exception);

        private readonly object lockObj = new object();
        private Process currentProcess;

        public void Launch(
            string exeFilePath,
            string saveDirectoryPath,
            bool showConsole,
            EndedCallback endedCallback)
        {
            if (showConsole)
            {
                LaunchWithConsole(exeFilePath, saveDirectoryPath, endedCallback);
            }
            else
            {
                LaunchWithoutConsole(exeFilePath, saveDirectoryPath, endedCallback);
            }
        }

        private void LaunchWithConsole(
            string exeFilePath, string saveDirectoryPath, EndedCallback endedCallback)
        {
            new Thread(() =>
            {
                try
                {
                    if (!File.Exists(exeFilePath))
                    {
                        throw new Exception("File not found: " + exeFilePath);
                    }

                    string fullExeFilePath = Path.GetFullPath(exeFilePath);
                    string exeContainedDir = Directory.GetParent(fullExeFilePath).FullName;

                    using (Process cmdProcess = new Process())
                    {
                        cmdProcess.StartInfo.FileName = "cmd.exe";
                        cmdProcess.StartInfo.WorkingDirectory = exeContainedDir;

                        if (saveDirectoryPath == null)
                        {
                            cmdProcess.StartInfo.Arguments =
                                "/C cmd.exe /C " +
                                "\"\"" + fullExeFilePath.Replace("\"", "\"\"") + "\"\" " +
                                "& echo. & echo Process exited with an exit code of %errorlevel%. " +
                                "& pause";
                        }
                        else
                        {
                            cmdProcess.StartInfo.Arguments =
                                "/C cmd.exe /C " +
                                "\"\"" + fullExeFilePath.Replace("\"", "\"\"") + "\" " +
                                "-savedirectory " +
                                "\"" + saveDirectoryPath.Replace("\"", "\"\"") + "\"\" " +
                                "& echo. & echo Process exited with an exit code of %errorlevel%. " +
                                "& pause";
                        }
                        cmdProcess.Start();

                        Process actualProcess;
                        while (true)
                        {
                            if (cmdProcess.HasExited)
                            {
                                throw new Exception("Process exited before child could be identified");
                            }

                            Process[] childProcesses = GetChildProcesses(cmdProcess);
                            if (childProcesses != null && childProcesses.Length > 0)
                            {
                                try
                                {
                                    foreach (Process childProcess in childProcesses)
                                    {
                                        Console.WriteLine(childProcess.MainModule.FileName);
                                    }
                                }
                                finally
                                {
                                    DisposeAll(childProcesses);
                                }
                            }

                            Thread.Sleep(100);
                        }

                        actualProcess.Kill();
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }).Start();
        }

        private void LaunchWithoutConsole(
            string exeFilePath, string saveDirectoryPath, EndedCallback endedCallback)
        {

        }

        public void TryKill()
        {

        }

        private static Process[] GetChildProcesses(Process process)
        {
            Process[] allRunningProcesses = Process.GetProcesses();
            if (allRunningProcesses == null)
            {
                return new Process[0];
            }
            else
            {
                List<Process> childProcesses = new List<Process>();
                try
                {
                    foreach (Process runningProcess in allRunningProcesses)
                    {
                        try
                        {
                            if (GetParentProcessId(runningProcess) == process.Id)
                            {
                                Process childProcess = Process.GetProcessById(runningProcess.Id);
                                childProcesses.Add(childProcess);
                                childProcesses.AddRange(GetChildProcesses(childProcess));
                            }
                        }
                        catch (Exception ignore) { }
                    }
                    return childProcesses.ToArray();
                }
                finally
                {
                    DisposeAll(allRunningProcesses);
                }
            }
        }

        private static void DisposeAll(IEnumerable<IDisposable> disposables)
        {
            Exception exception = null;
            foreach (IDisposable disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }
            if (exception != null)
            {
                throw new Exception("Error disposing one or more disposables", exception);
            }
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr ProcessHandle,
            int ProcessInformationClass,
            ref PROCESS_BASIC_INFORMATION ProcessInformation,
            uint ProcessInformationLength,
            out uint ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public int ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public int BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        private static int GetParentProcessId(Process process)
        {
            IntPtr processHandle = process.Handle;
            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
            int result = NtQueryInformationProcess(
                processHandle, 0, ref pbi, (uint)Marshal.SizeOf(pbi), out _);
            if (result != 0)
            {
                throw new Exception("NtQueryInformationProcess failed");
            }
            return (int)pbi.InheritedFromUniqueProcessId;
        }

    }
}