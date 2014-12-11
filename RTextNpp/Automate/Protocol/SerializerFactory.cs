using System.Runtime.Serialization.Json;

namespace RTextNppPlugin.RTextEditor.Protocol
{
    public class SerializerFactory<T>
    {        
        public static DataContractJsonSerializer getSerializer()
        {
            return new DataContractJsonSerializer(typeof(T));
        }
    }
}
