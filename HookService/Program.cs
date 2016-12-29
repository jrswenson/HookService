using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HookService
{
    static class Program
    {
        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var service = new Service();
            if (Environment.UserInteractive)
            {
                AllocConsole();

                Task.Factory.StartNew(() =>
                {
                    service.StartIt(new string[] { });
                });

                Console.WriteLine("Press Any Key to Exit:");
                Console.ReadKey(false);
                service.StopIt();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { service });
            }
        }
    }
}
