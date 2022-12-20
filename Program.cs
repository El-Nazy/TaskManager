using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace TaskManager
{
    static class Program
    {
        static StringBuilder notification = new StringBuilder();
        static List<Thread> threads = new List<Thread>();

        static void Main()
        {
            Console.Clear();
            Console.WriteLine(
                GetAndClearNotifications() +

                "MAIN MENU\n" +
                "-----------------------------------------------\n" +
                "PRESS \"a\" to VIEW all PROCESSES\n" +
                "PRESS \"b\" to SELECT a PROCESS to KILL\n" +
                "PRESS \"c\" to CREATE a new PROCESS\n\n" +

                "PRESS \"d\" to CREATE a new THREAD\n" +
                "PRESS \"e\" to VIEW all the THREADS you created\n" +
                "PRESS \"f\" to SELECT a THREAD to KILL\n"
            );

        TakeInput:
            switch (Console.ReadKey(true).KeyChar.ToString().ToLower())
            {
                case "a":
                    ShowAllProcesses();
                    return;
                case "b":
                    ShowMenuToKillProcess();
                    return;
                case "c":
                    ShowPromptToCreateProcess();
                    return;

                case "d":
                    ShowPromptToCreateThread();
                    return;
                case "e":
                    ShowAllCreatedThreads();
                    return;
                case "f":
                    ShowPromptToSelectAThreadToAbort();
                    return;
                default:
                    goto TakeInput;
            }
        }

        static string GetAndClearNotifications()
        {
            var result = (notification.Length == 0 ? "" : ("NOTIFICATION(S):\n" + notification + "\n"));

            // Clear Notifications
            notification.Clear();

            return result;
        }

        static void ShowAllProcesses()
        {
        Start:
            Console.Clear();
            Console.WriteLine(
                "List of Processes\n" +
                "--------------------------------\n" +
                GetNumberedListOfAllProcesses() +

                "\n\n" +

                GetAndClearNotifications() +
                "PRESS \"b\" to go BACK to the MAIN MENU\n" +
                "PRESS \"k\" to select a PROCESS to KILL\n" +
                "PRESS \"any other key\" to REFRESH this LIST"
            );

            // Collect key input and navigate
            switch (Console.ReadKey().KeyChar.ToString().ToLower())
            {
                case "b":
                    Main();
                    return;
                case "k":
                    ShowMenuToKillProcess();
                    return;
                default:
                    goto Start;
            }
        }

        static string GetNumberedListOfAllProcesses()
        {
            var result = new StringBuilder();

            result.Append("PIDs\tProcess Names\n");
            var processes = Process.GetProcesses().OrderBy(process => process.Id);

            foreach (var process in processes)
            {
                result.Append($"{process.Id}\t{process.ProcessName}\n");
            }

            return result.ToString();
        }

        static void ShowMenuToKillProcess()
        {
        Start:
            Console.Clear();
            Console.WriteLine(
                GetNumberedListOfAllProcesses() +

                "\n\n" +

                GetAndClearNotifications() +

                "ENTER the PID or NAME of the PROCESS(ES) to be KILLED\n" +
                "(NB: PID takes precedence over NAME in case of a conflict)\n" +
                "Or PRESS \"enter\" to go BACK to the MAIN MENU\n"
            );

            // Collect key input and perform action
            var input = Console.ReadLine();

            if (String.IsNullOrEmpty(input))
            {
                Main();
            }

            try
            {
                var process = Process.GetProcessById(int.Parse(input));
                process.Kill();
                notification.Append($"Process with PID `{process.Id}` and name `{process.ProcessName}` was killed successfully\n");
            }
            // Catch ArgumentException from Process.GetProcessById
            catch (Exception ex) when (ex is ArgumentException ||
            // Catch FormatException and OverflowException from int.Parse
                                        ex is FormatException ||
                                        ex is OverflowException)
            {
                var processes = Process.GetProcessesByName(input);
                if (processes.Length == 0)
                {
                    notification.Append($"Failed to kill the process(es) you identified with `{input}`\n");
                    goto Start;
                }

                foreach (var process in processes)
                {
                    process.Kill();
                    notification.Append($"Process with PID {process.Id} name {process.ProcessName}" +
                        " was killed successfully\n");
                }
            }

            goto Start;
        }

        static void ShowPromptToCreateProcess()
        {
        Start:
            Console.Clear();
            Console.WriteLine(
                GetAndClearNotifications() +

                "Create Process\n" +
                "---------------------------------------------\n" +
                "ENTER a valid PATH to a FILE or EXECUTABLE\n" +
                "Or PRESS \"enter\" to go back\n"
            );

            var input = Console.ReadLine();

            if (String.IsNullOrEmpty(input))
            {
                Main();
            }

            try
            {
                var process = Process.Start(input);
                notification.Append($"Process started successfully with PID \"{process.Id}\"\n");
                Main();
            }
            catch (Exception)
            {
                notification.Append($"Failed to start a process with the path \"{input}\"; try again\n");
                goto Start;
            }
        }

        static void ShowPromptToCreateThread()
        {
        Start:
            Console.Clear();
            Console.WriteLine(
                GetAndClearNotifications() +

                "Create Thread\n" +
                "---------------------------------------------\n" +
                "ENTER a NAME for the THREAD\n" +
                "Or PRESS \"enter\" to go BACK to the MAIN MENU\n"
            );

            var input = Console.ReadLine();

            if (String.IsNullOrEmpty(input))
            {
                Main();
                return;
            }

            // Create infinitely sleeping thread
            var thread = new Thread(new ThreadStart(() => Thread.Sleep(Timeout.Infinite)));
            thread.Name = input;

            Console.WriteLine(
                "\nPRESS \"b\" to CREATE the THREAD in the BACKGROUND\n" +
                "PRESS \"f\" to CREATE the THREAD in the FOREGROUND\n" +
                "PRESS \"enter\" to go BACK"
            );

        TakeOption:
            switch (Console.ReadKey(true).KeyChar.ToString().ToLower())
            {
                case "b":
                    thread.IsBackground = true;
                    goto case "f";
                case "f":
                    thread.Start();
                    notification.Append($"Thread \"{input}\" create successfully\n");
                    threads.Add(thread);
                    Main();
                    return;
                case "\r":
                    goto Start;
                default:
                    goto TakeOption;
            }
        }

        static void ShowAllCreatedThreads()
        {
        Start:
            Console.Clear();
            Console.WriteLine(
                GetNumberedListOfAllCreatedThreads() +

                $"\n\n" +

                GetAndClearNotifications() +

                (threads.Any() ? "ENTER the S/N of the THREAD to VIEW more DETAILS\n" :
                    "PRESS \"c\" to create a THREAD\n") +
                "Or PRESS \"enter\" to go BACK to the MAIN MENU"
            );

            if (threads.Any())
            {
                var input = Console.ReadLine();

                if (String.IsNullOrEmpty(input))
                {
                    Main();
                }

                Thread thread;
                try
                {
                    thread = threads[int.Parse(input) - 1];
                }
                catch (Exception)
                {
                    notification.Append("You entered an invalid S/N; try again\n");
                    goto Start;
                }
                ShowThreadDetails(thread);
            }
            AskToShowPromptToCreateThreadOrShowMain();
        }

        static void ShowThreadDetails(Thread thread)
        {
            Console.Clear();
            Console.WriteLine(
                GetAndClearNotifications() +
                $"Thread Details\n" +
                "---------------------------------------------\n" +
                $"Name:\t\t{thread.Name}\n" +
                $"IsAlive:\t{thread.IsAlive}\n" +
                $"IsBackground:\t{thread.IsBackground}\n\n" +

                (thread.IsAlive ? "PRESS \"a\" to ABORT this THREAD\n" : "") +
                "PRESS \"b\" to go BACK\n"
            );

        TakeOption:
            switch (Console.ReadKey(true).KeyChar.ToString().ToLower())
            {
                case "a":
                    TryAbortThread(thread);
                    ShowAllCreatedThreads();
                    return;
                case "b":
                    ShowAllCreatedThreads();
                    return;
                default:
                    goto TakeOption;
            }
        }

        static void TryAbortThread(Thread thread, string S_N = null)
        {
            if (thread.IsAlive)
            {
                try
                {
                    thread.Abort();
                    threads.Remove(thread);
                    notification.Append($"THREAD with {(S_N is null ? "" : $"S/N `{S_N}` and ")}name `{thread.Name}` was aborted successfully\n");
                }
                catch (Exception ex)
                {
                    notification.Append($"Unable to abort THREAD with name `{thread.Name}` cause;\n{ex.Message}\n");
                }
            }
        }

        static string GetNumberedListOfAllCreatedThreads()
        {
            var result = new StringBuilder();

            if (!threads.Any())
            {
                return "No threads to show\n";
            }

            result.Append("S/N\tManagedID\tName(s)\n");
            for (int i = 0; i < threads.Count; i++)
            {
                result.Append($"{i + 1}\t{threads[i].ManagedThreadId}\t\t{threads[i].Name}\n");
            }

            return result.ToString();
        }

        static void ShowPromptToSelectAThreadToAbort()
        {
        Start:
            Console.Clear();

            Console.WriteLine(
                GetNumberedListOfAllCreatedThreads() +

                $"\n\n" +

                GetAndClearNotifications() +

                (threads.Any() ? "ENTER the S/N of the THREAD to VIEW more DETAILS\n" +
                    "(NB: S/N takes precedence over NAME in case of a conflict)\n"
                :
                    "PRESS \"c\" to create a THREAD\n") +
                "0r PRESS \"enter\" to go BACK\n"
            );

            if (threads.Any())
            {
                // Collect key input and perform action
                var input = Console.ReadLine();

                if (String.IsNullOrEmpty(input))
                {
                    Main();
                }

                try
                {
                    var thread = threads[int.Parse(input) - 1];
                    TryAbortThread(thread, input);
                }
                catch (Exception ex) when (ex is ArgumentOutOfRangeException ||
                                           ex is FormatException ||
                                           ex is OverflowException)
                {
                    var threadsToAbort = threads.Where(thread => thread.Name == input);
                    if (!threadsToAbort.Any())
                    {
                        notification.Append($"Could not identify any thread with `{input}`\n");
                        goto Start;
                    }

                    foreach (var thread in threadsToAbort)
                    {
                        TryAbortThread(thread);
                    }
                }

                goto Start;
            }

            AskToShowPromptToCreateThreadOrShowMain();
        }

        static void AskToShowPromptToCreateThreadOrShowMain()
        {
        TakeOption:
            switch (Console.ReadKey().KeyChar.ToString().ToLower())
            {
                case "c":
                    ShowPromptToCreateThread();
                    return;
                case "\r":
                    Main();
                    return;
                default:
                    goto TakeOption;
            }
        }
    }
}
