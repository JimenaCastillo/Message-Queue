using System.Text.Json;

namespace MessageQueue.Common
{
    // Tipo de operación que se enviará al broker
    public enum OperationType
    {
        Subscribe,
        Unsubscribe,
        Publish,
        Receive
    }

    // Estructura del mensaje que se enviará a través del socket
    public class ProtocolMessage
    {
        public OperationType Operation { get; set; }
        public Guid AppId { get; set; }
        public string Topic { get; set; }
        public string Content { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        // Serializa el mensaje a JSON para enviarlo por el socket
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        // Deserializa el mensaje recibido del socket
        public static ProtocolMessage Deserialize(string json)
        {
            return JsonSerializer.Deserialize<ProtocolMessage>(json);
        }
    }
}