using System;
using System.Windows.Forms;

namespace LicenseManagement
{
    public partial class SplashScreen : Form
    {
        private System.Windows.Forms.Label labelProgress;

        private int elapsed = 0;
        private int duration = 3000; // durée totale en millisecondes

        public SplashScreen()
        {
            InitializeComponent();

            // Initialisation de la barre de progression
            progressBar1.Maximum = duration;
            progressBar1.Value = 0;

            // Initialisation du timer
            timer1.Interval = 20;
            timer1.Tick += Timer1_Tick;
        }

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }


        private void Timer1_Tick(object sender, EventArgs e)
        {
            elapsed += timer1.Interval;

            if (elapsed >= duration)
            {
                timer1.Stop();
                this.Hide();               // Cache le SplashScreen
                new Form1().Show();        // Ouvre la page Form1
            }
            else
            {
                // Animation douce : progression + effet de transparence "pulse"
                progressBar1.Value = Math.Min(elapsed, duration);
                this.Opacity = 0.9 + 0.1 * Math.Sin((double)elapsed / duration * Math.PI);
            }
        }
    }
}
