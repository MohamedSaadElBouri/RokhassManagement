using System;
using System.Windows.Forms;

namespace LicenseManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Si quelque part tu utilises |DataDirectory|, on le pointe sur le dossier de l'exe
            AppDomain.CurrentDomain.SetData("DataDirectory", Application.StartupPath);

            // Création de la DB + table Licenses si besoin
            Database.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);// Pour le moteur de rendu du texte 'false' c'est plus moderne
            Application.Run(new SplashScreen());
        }
    }
}
