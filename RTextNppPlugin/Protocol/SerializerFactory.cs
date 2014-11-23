using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;

namespace ESRLabs.RTextEditor.Protocol
{
    public class SerializerFactory<T>
    {        
        public static DataContractJsonSerializer getSerializer()
        {
            return new DataContractJsonSerializer(typeof(T));
        }
    }
}
