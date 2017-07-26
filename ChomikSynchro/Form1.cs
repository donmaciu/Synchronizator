using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChomikSynchro.Chomik;
using Chomik;

namespace ChomikSynchro
{
    public partial class Form1 : Form
    {
        int ExitCode { get; set; }
        public Form1()
        {
            InitializeComponent();
            this.Visible = false;
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            try
            {
                Logger.Log("Rozpoczynanie sesji");

                Synchronizer sync = new Synchronizer();
                sync.Synchronize();

                Logger.Log("Koniec sesji");
            } catch (Exception exc)
            {
                ExitCode = 1;
                Logger.Log(exc.Message);
            }
            finally
            {
                Environment.Exit(ExitCode);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
    }
}
