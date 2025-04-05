using MessageQueue.Common;
using System.Net.Sockets;
using System.Text;

namespace MessageQueue.Client
{
    public class MQClient
    {
        private readonly string _serverIp;
        private readonly int _serverPort;
        private readonly Guid _appId;

        public MQClient(string ip, int port, Guid appId)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentException("La dirección IP no puede estar vacía", nameof(ip));

            if (port <= 0 || port > 65535)
                throw new ArgumentException("El puerto debe estar entre 1 y 65535", nameof(port));

            if (appId == Guid.Empty)
                throw new ArgumentException("El ID de la aplicación no puede estar vacío", nameof(appId));

            _serverIp = ip;
            _serverPort = port;
            _appId = appId;
        }

        public bool Subscribe(Topic topic)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            var request = new ProtocolMessage
            {
                Operation = OperationType.Subscribe,
                AppId = _appId,
                Topic = topic.Name
            };

            var response = SendRequest(request);
            return response.Success;
        }

        public bool Unsubscribe(Topic topic)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            var request = new ProtocolMessage
            {
                Operation = OperationType.Unsubscribe,
                AppId = _appId,
                Topic = topic.Name
            };

            var response = SendRequest(request);
            return response.Success;
        }

        public bool Publish(Message message, Topic topic)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            var request = new ProtocolMessage
            {
                Operation = OperationType.Publish,
                AppId = _appId,
                Topic = topic.Name,
                Content = message.Content
            };

            var response = SendRequest(request);
            return response.Success;
        }

        public Message Receive(Topic topic)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            var request = new ProtocolMessage
            {
                Operation = OperationType.Receive,
                AppId = _appId,
                Topic = topic.Name
            };

            var response = SendRequest(request);

            if (!response.Success)
                throw new InvalidOperationException(response.ErrorMessage ?? "No se pudo recibir el mensaje");

            return new Message(response.Content);
        }

        private ProtocolMessage SendRequest(ProtocolMessage request)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Conectar al servidor
                    client.Connect(_serverIp, _serverPort);

                    var stream = client.GetStream();

                    // Serializar y enviar el mensaje
                    var requestData = Encoding.UTF8.GetBytes(request.Serialize());
                    stream.Write(requestData, 0, requestData.Length);

                    // Recibir la respuesta
                    var responseBuffer = new byte[4096];
                    int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                    var responseData = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                    // Deserializar la respuesta
                    return ProtocolMessage.Deserialize(responseData);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al comunicarse con el servidor: {ex.Message}", ex);
            }
        }
    }
}