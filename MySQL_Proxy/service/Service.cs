using System;
using System.Collections.Generic;
using MySQL_Proxy.handler;

namespace MySQL_Proxy.service
{
    class Service
    {
        private readonly List<Handler> handlerList = new List<Handler>();

        public void Create(IAsyncResult ar)
        {
            Handler handler = new Handler(ar);


            handlerList.Add(handler);
        }
    }
}
