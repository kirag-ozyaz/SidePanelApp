using System;
using SidePanelApp;

class GenerateAppIcon
{
    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : "..\\Assets\\app.ico";
        AppIconFactory.SaveIconFile(path);
        Console.WriteLine("Saved: " + System.IO.Path.GetFullPath(path));
    }
}
