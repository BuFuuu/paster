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
        Console.WriteLine("Usage: paster -p FILE -w WINDOW [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -L, --list       List open window titles");
        Console.WriteLine("  -A, --after SEC  Delay after window activation (default 1)");
        Console.WriteLine("  -D, --delay SEC  Delay between chunks (default 1)");
        Console.WriteLine("  -C, --chunk KB   Chunk size in KB (default 800)");
        Console.WriteLine("  -p, --path FILE  File to send");
        Console.WriteLine("  -w, --window T   Target window title");
        Console.WriteLine("  -m, --manual     Do not auto focus the window");
        Console.WriteLine("  -S, --shift      Use Ctrl+Shift+V for pasting");
        Console.WriteLine("  -E, --echo FILE  Wrap chunk in echo >> FILE and press Enter");
        Console.WriteLine();
        Console.WriteLine("Example: paster -p data.zip -w \"Terminal\" -C 60 -D 0.5");
        Console.WriteLine();
        Console.WriteLine("This program base64 encodes a file and pastes it into the target window in chunks.");
    }

    static void ShowWindowInfo()
    {
        Console.WriteLine("PowerShell command to list open windows:");
        Console.WriteLine("Get-Process | Where-Object { $_.MainWindowTitle -ne \"\" } | ForEach-Object { $_.MainWindowTitle }");
        Console.WriteLine();
        Console.WriteLine("Base64 decode examples:");
        Console.WriteLine("powershell:\t[System.IO.File]::WriteAllBytes('out.zip', [System.Convert]::FromBase64String([System.IO.File]::ReadAllText('transferred.b64')))");
        Console.WriteLine("bash:\t\tbase64 -d transferred.b64 > out.zip");
    }

    [STAThread]
    static void Main(string[] args)
    {
        bool listWindows = false;
        double sleepAfterFirstWindow = 1.0;
        double sleepBetweenChunks = 1.0;
        int chunkSize = 800; // Default chunk size in KB
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
                case "-L":
                case "--list":
                    listWindows = true;
                    break;
                case "-A":
                case "--after":
                    if (i + 1 < args.Length) { sleepAfterFirstWindow = double.Parse(args[++i], CultureInfo.InvariantCulture); }
                    break;
                case "-D":
                case "--delay":
                    if (i + 1 < args.Length) { sleepBetweenChunks = double.Parse(args[++i], CultureInfo.InvariantCulture); }
                    break;
                case "-C":
                case "--chunk":
                    chunkSize = int.Parse(args[++i]);
                    break;
                case "-p":
                case "--path":
                    filePath = args[++i];
                    break;
                case "-w":
                case "--window":
                    targetWindowTitle = args[++i];
                    break;
                case "-m":
                case "--manual":
                    manualActivation = true;
                    break;
                case "-S":
                case "--shift":
                    useShiftPaste = true;
                    break;
                case "-E":
                case "--echo":
                    if (i + 1 < args.Length) { echoWrapFilename = args[++i]; } else { Console.WriteLine("echo requires a file name!"); return; }
                    break;
            }
        }

        if (listWindows)
        {
            ShowWindowInfo();
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
            ShowWindowInfo();
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
