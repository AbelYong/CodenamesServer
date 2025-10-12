using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Host
{
    internal class Program
    {
        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(Services.AuthenticationService)))
            {
                host.Open();

                Console.CancelKeyPress += (sender, eArgs) => {
                    eArgs.Cancel = true;
                    _quitEvent.Set();
                };
                Console.WriteLine("=== Codenames: Duet - Server ===");
                Console.WriteLine("Ejecutando... presiona Ctrl+C para salir.");
                _quitEvent.WaitOne();
            }
        }
    }
}
