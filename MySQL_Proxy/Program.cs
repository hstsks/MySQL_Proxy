using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySQL_Proxy.server;

namespace MySQL_Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            Server.StartListening(11006);
        }
    }
}
