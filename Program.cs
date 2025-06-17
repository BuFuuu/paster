using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;

// Author: Lion Hellstern aka. BuFu
class Program
{
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    static void PrintHelp()
    {
        Console.WriteLine("Usage: YourApplication [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --list-windows, -l\t\tList all open windows titles");
        Console.WriteLine("  --sleep-after-first-window, -s\tSleep time after first window activation. Default is 1.0 seconds");
        Console.WriteLine("  --sleep-between-chunks, -sb\tSleep time between chunks. Default is 1.0 seconds (1s)");
        Console.WriteLine("  --chunk-size, -c\t\tSize of each chunk in KB. Default is 800 (0.8MB)");
        Console.WriteLine("  --file-path, -f\t\tPath to the file to be processed");
        Console.WriteLine("  --target-window-title, -t\tTitle of the target window");
        Console.WriteLine("  --manual-activation, -m\tDo not activate the window. Just start pasting. User needs to activate the window");
        Console.WriteLine("  --shift-paste, -sp\tUse Ctrl+Shift+V to paste instead of Ctrl+V");
        Console.WriteLine("  --echo-wrap, -ew\tWrap in a echo -n \"base64 content\" >> filename statement and send RETURN after pasting");

        Console.WriteLine("\nThis program base64 encodes a file, then pastes it into an activated window using 'Ctrl+V'. Ideal for transferring files to text-restricted inputs, it divides the content into manageable chunks. Users should zip files first for efficiency. The chunk size and paste interval can be adjusted with '-c' and '-sb' options to suit different needs, ensuring a smooth and customizable file transfer process.");
    }

    [STAThread]
    static void Main(string[] args)
    {
        bool listWindows = false;
        double sleepAfterFirstWindow = 1.0;
        double sleepBetweenChunks = 1.0;
        int chunkSize = 800; // Default chunk size in bytes
        string filePath = null;
        string targetWindowTitle = null;
        bool manualActivation = false;
        bool useShiftPaste = false;
        string echoWrapFilename = null;

        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            PrintHelp();
            return;
        }

        // Simple command-line argument parsing
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-l":
                case "--list-windows":
                    listWindows = true;
                    break;
                case "-s":
                case "--sleep-after-first-window":
                    if (i + 1 < args.Length) { sleepAfterFirstWindow = double.Parse(args[++i], CultureInfo.InvariantCulture); }
                    break;
                case "-sb":
                case "--sleep-between-chunks":
                    if (i + 1 < args.Length) { sleepBetweenChunks = double.Parse(args[++i], CultureInfo.InvariantCulture); }
                    break;
                case "-c":
                case "--chunk-size":
                    chunkSize = int.Parse(args[++i]);
                    break;
                case "-f":
                case "--file-path":
                    filePath = args[++i];
                    break;
                case "-t":
                case "--target-window-title":
                    targetWindowTitle = args[++i];
                    break;
                case "-m":
                case "--manual-activation":
                    manualActivation = true;
                    break;
                case "-sp":
                case "--shift-paste":
                    useShiftPaste = true;
                    break;
                case "-ew":
                case "--echo-wrap":
                    if (i + 1 < args.Length) { echoWrapFilename = args[++i]; } else { Console.WriteLine("echo-wrap needs a file name!"); return; }
                    break;
            }
        }

        if (listWindows)
        {
            Console.WriteLine("PowerShell command to list open windows:");
            Console.WriteLine("Get-Process | Where-Object { $_.MainWindowTitle -ne \"\" } | ForEach-Object { $_.MainWindowTitle }");
            Console.WriteLine("");
            Console.WriteLine("Base64 decode examples:");
            Console.WriteLine("powershell:\t[System.IO.File]::WriteAllBytes('out.zip', [System.Convert]::FromBase64String([System.IO.File]::ReadAllText('transfered.b64')))");
            Console.WriteLine("bash:\t\tbase64 -d transfered.b64 > out.zip");
            return; // Exit the program
        }

        if (filePath == null)
        {
            Console.WriteLine("File path is required.");
            return;
        }

        RunProgram(listWindows, sleepAfterFirstWindow, sleepBetweenChunks, chunkSize, filePath, targetWindowTitle, manualActivation, useShiftPaste, echoWrapFilename);
    }

    static void RunProgram(bool listWindows, double sleepAfterFirstWindow, double sleepBetweenChunks, int chunkSize, string filePath, string targetWindowTitle, bool manualActivation, bool useShiftPaste, string echoWrapFilename)
    {
        chunkSize = chunkSize * 1024;

        if (listWindows)
        {
            Console.WriteLine("PowerShell command to list open windows:");
            Console.WriteLine("Get-Process | Where-Object { $_.MainWindowTitle -ne \"\" } | ForEach-Object { $_.MainWindowTitle }");
            Console.WriteLine("");
            Console.WriteLine("Base64 decode examples:");
            Console.WriteLine("powershell:\t[System.IO.File]::WriteAllBytes('out.zip', [System.Convert]::FromBase64String([System.IO.File]::ReadAllText('transfered.b64')))");
            Console.WriteLine("bash:\t\tbase64 -d transfered.b64 > out.zip #4664");
            return; // Exit the program
        }

        var encodedContent = CompressAndEncode(filePath);
        var totalLength = encodedContent.Length;

        ActivateWindow(targetWindowTitle, manualActivation);
        Thread.Sleep(TimeSpan.FromSeconds(sleepAfterFirstWindow));

        int processedLength = 0;
        while (processedLength < totalLength)
        {
            var currentChunk = encodedContent.Substring(processedLength, Math.Min(chunkSize, totalLength - processedLength));
            if (!CopyPasteChunk(currentChunk, targetWindowTitle, sleepBetweenChunks, manualActivation, useShiftPaste, echoWrapFilename))
            {
                Console.WriteLine("Could not find or focus the window.");
                Environment.Exit(1);
            }
            processedLength += currentChunk.Length;
            UpdateProgressBar(processedLength, totalLength);
        }
    }



    static string CompressAndEncode(string filePath)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);
        return Convert.ToBase64String(fileBytes);
    }

    static bool ActivateWindow(string windowTitle, bool manualActivation)
    {
        if (manualActivation) { return true; }
        IntPtr hWnd = FindWindow(null, windowTitle);
        if (hWnd != IntPtr.Zero)
        {
            SetForegroundWindow(hWnd);
            return true;
        }
        return false;
    }

    static bool CopyPasteChunk(string text, string windowTitle, double sleepDuration, bool manualActivation, bool useShiftPaste, string echoWrapFilename)
    {
        bool echowrap = !string.IsNullOrEmpty(echoWrapFilename);
        if (echowrap) { text = $"echo -n \"{text}\" >> {echoWrapFilename}"; }

        Clipboard.SetText(text);

        if (ActivateWindow(windowTitle, manualActivation))
        {
            SendKeys.SendWait(useShiftPaste ? "^+{V}" : "^{V}"); // "^+{V}" for Ctrl+Shift+V
            Thread.Sleep(TimeSpan.FromSeconds(sleepDuration));

            if (echowrap) { SendKeys.SendWait("{ENTER}"); }
            Thread.Sleep(TimeSpan.FromSeconds(0.3));

            return true;
        }
        return false;
    }


    static void UpdateProgressBar(long transferred, long total)
    {
        int barSize = 50; // Size of the progress bar in characters
        double pctComplete = (double)transferred / total;
        int chars = (int)Math.Round(pctComplete * barSize);

        string bar = new string('=', chars) + new string(' ', barSize - chars);
        string sizeInfo = $"{FormatBytes(transferred)} / {FormatBytes(total)}";

        Console.CursorLeft = 0;
        Console.Write($"[{bar}] {pctComplete:P0} - {sizeInfo} ({total} bytes)");
    }

    static string FormatBytes(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        double size = bytes;
        int i = 0;

        while (i < suffix.Length - 1 && size >= 1024)
        {
            size /= 1024;
            i++;
        }

        return $"{size:0.##} {suffix[i]}";
    }
}
