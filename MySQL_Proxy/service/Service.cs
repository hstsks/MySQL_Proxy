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
            handler.onClose = new OnClose(OnHandlerClose);

            handlerList.Add(handler);
        }

        private void OnHandlerClose(string id)
        {
            if (id == null)
            {
                return;
            }
            Handler target = handlerList.Find(x => x.handlerID.Contains(id));

            if(target != null)
            {
                handlerList.Remove(target);
            }
        }
    }
}
