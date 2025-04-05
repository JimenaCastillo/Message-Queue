using System.Text.Json;

namespace MessageQueue.Client
{
    public class Message
    {
        public string Content { get; }
        public DateTime Timestamp { get; }

        public Message(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            Content = content;
            Timestamp = DateTime.UtcNow;
        }

        public Message(string content, DateTime timestamp)
        {
            Content = content;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"[{Timestamp}] {Content}";
        }

        // Serializa el mensaje a JSON
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        // Deserializa el mensaje de JSON
        public static Message Deserialize(string json)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<MessageDTO>(json);
                return new Message(obj.Content, obj.Timestamp);
            }
            catch
            {
                // Si falla la deserialización, intenta usar el contenido directo
                return new Message(json);
            }
        }

        // Clase interna para deserialización
        private class MessageDTO
        {
            public string Content { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}