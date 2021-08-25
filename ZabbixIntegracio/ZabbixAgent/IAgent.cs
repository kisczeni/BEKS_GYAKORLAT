using System;
using System.Collections.Generic;
using System.Text;

namespace ZabbixAgent {
    public interface IAgent {
        void Start();
        event EventHandler<ZabbixEventArgs> ZabbixEventHappened;
    }
}
