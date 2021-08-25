using System;
using System.Collections.Generic;
using System.Text;

namespace JSONClasses {
    public class AgentData {
        public string request { get; set; }
        public List<Data> data { get; set; }
        public long clock { get; set; }
        public int ns { get; set; }

        public class Data {
            public string host { get; set; }
            public string key { get; set; }
            public object value { get; set; }
            public long clock { get; set; }
            public int ns { get; set; }
        }
    }
}
