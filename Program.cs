using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace _2017Launcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            Application.Run(new Form1());
        }
        catch (Exception exception)
        {
            string error = exception.ToString();
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt"), error);
            }
            catch
            {
            }
            MessageBox.Show(error, "LuckyBlox Launcher startup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
