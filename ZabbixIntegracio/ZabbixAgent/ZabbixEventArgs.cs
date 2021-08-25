using System;
using System.Collections.Generic;
using System.Text;

namespace ZabbixAgent {
    public class ZabbixEventArgs : EventArgs {
        public string key { get; internal set; }

        public string value { get; set; }
    }
}
