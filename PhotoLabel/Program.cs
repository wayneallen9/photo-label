using System;
using System.Threading;
using System.Windows.Forms;
namespace PhotoLabel
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // restrict the size of the threadpool for loading the preview images
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // create the starting form
            var formMain = NinjectKernel.Get<FormMain>();

            // run the application
            Application.Run(formMain);
        }
    }
}
