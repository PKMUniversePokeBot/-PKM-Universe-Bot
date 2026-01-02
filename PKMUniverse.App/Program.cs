// PKM Universe Bot - Program Entry
// Written by PKM Universe - 2025

using PKMUniverse.App.Forms;

namespace PKMUniverse.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
