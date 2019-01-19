using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace stLib_CS {
    namespace Controller {
        public class XMLController {
            protected XmlDocument document = new XmlDocument();            
            public virtual void Load() { }
            public virtual void Save() { }
        }
    }
}
