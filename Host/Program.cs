using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Host
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(Services.AuthenticationService)))
            {
                host.Open();
                Console.WriteLine("=== Codenames: Duet - Server ===");
                Console.ReadLine();
            }
        }
    }
}
