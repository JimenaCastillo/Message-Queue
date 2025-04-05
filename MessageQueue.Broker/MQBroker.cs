using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MessageQueue.Common;
using MessageQueue.DataStructures;

namespace MessageQueue.Broker
{
    public class MQBroker
    {
        private TcpListener _server;
        private readonly int _port = 8888;
        private bool _isRunning = false;

        // Almacena las suscripciones: Topic -> Lista de AppIds suscritos
        private readonly HashMap<string, LinkedList<Guid>> _topicSubscriptions;

        // Almacena las colas de mensajes: (AppId, Topic) -> Cola de mensajes
        private readonly HashMap<string, Queue<string>> _queues;

        public MQBroker()
        {
            _topicSubscriptions = new HashMap<string, LinkedList<Guid>>();
            _queues = new HashMap<string, Queue<string>>();
        }

        public async Task Start()
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, _port);
                _server.Start();
                _isRunning = true;

                Console.WriteLine($"MQBroker escuchando en el puerto {_port}");

                while (_isRunning)
                {
                    // Espera conexiones entrantes
                    var client = await _server.AcceptTcpClientAsync();
                    Console.WriteLine("Cliente conectado");

                    // Procesa la conexión en un hilo separado
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en el servidor: {ex.Message}");
            }
            finally
            {
                _server?.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];

                try
                {
                    // Lee datos del cliente
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var messageData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Deserializa el mensaje
                    var message = ProtocolMessage.Deserialize(messageData);

                    // Procesa el mensaje según el tipo de operación
                    ProtocolMessage response = null;

                    switch (message.Operation)
                    {
                        case OperationType.Subscribe:
                            response = HandleSubscribe(message);
                            break;
                        case OperationType.Unsubscribe:
                            response = HandleUnsubscribe(message);
                            break;
                        case OperationType.Publish:
                            response = HandlePublish(message);
                            break;
                        case OperationType.Receive:
                            response = HandleReceive(message);
                            break;
                        default:
                            response = new ProtocolMessage
                            {
                                Success = false,
                                ErrorMessage = "Operación no soportada"
                            };
                            break;
                    }

                    // Envía la respuesta al cliente
                    var responseBytes = Encoding.UTF8.GetBytes(response.Serialize());
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al procesar la solicitud del cliente: {ex.Message}");

                    // Envía el error al cliente
                    var errorResponse = new ProtocolMessage
                    {
                        Success = false,
                        ErrorMessage = "Error interno del servidor"
                    };
                    var errorBytes = Encoding.UTF8.GetBytes(errorResponse.Serialize());
                    await stream.WriteAsync(errorBytes, 0, errorBytes.Length);
                }
            }
        }

        private ProtocolMessage HandleSubscribe(ProtocolMessage request)
        {
            var appId = request.AppId;
            var topic = request.Topic;

            Console.WriteLine($"Solicitud de suscripción: AppId={appId}, Topic={topic}");

            // Crear clave para la cola
            string queueKey = $"{appId}_{topic}";

            // Añadir AppId a la lista de suscriptores para el tema
            var subscribers = _topicSubscriptions.GetOrAdd(topic, key => new LinkedList<Guid>());

            if (subscribers.Contains(appId))
            {
                return new ProtocolMessage
                {
                    Operation = OperationType.Subscribe,
                    Success = false,
                    ErrorMessage = "Ya está suscrito a este tema"
                };
            }

            // Añadir al conjunto de suscriptores
            subscribers.Add(appId);

            // Crear cola para este AppId y tema
            _queues.Add(queueKey, new Queue<string>());

            return new ProtocolMessage
            {
                Operation = OperationType.Subscribe,
                Success = true,
                Topic = topic
            };
        }

        private ProtocolMessage HandleUnsubscribe(ProtocolMessage request)
        {
            var appId = request.AppId;
            var topic = request.Topic;

            Console.WriteLine($"Solicitud de cancelación de suscripción: AppId={appId}, Topic={topic}");

            // Crear clave para la cola
            string queueKey = $"{appId}_{topic}";

            // Verificar si el tema existe
            if (!_topicSubscriptions.TryGetValue(topic, out var subscribers))
            {
                return new ProtocolMessage
                {
                    Operation = OperationType.Unsubscribe,
                    Success = false,
                    ErrorMessage = "No está suscrito a este tema"
                };
            }

            // Verificar si el AppId está suscrito
            if (!subscribers.Contains(appId))
            {
                return new ProtocolMessage
                {
                    Operation = OperationType.Unsubscribe,
                    Success = false,
                    ErrorMessage = "No está suscrito a este tema"
                };
            }

            // Eliminar AppId de la lista de suscriptores
            subscribers.Remove(appId);

            // Eliminar la cola para este AppId y tema
            _queues.Remove(queueKey);

            return new ProtocolMessage
            {
                Operation = OperationType.Unsubscribe,
                Success = true,
                Topic = topic
            };
        }

        private ProtocolMessage HandlePublish(ProtocolMessage request)
        {
            var appId = request.AppId;
            var topic = request.Topic;
            var content = request.Content;

            Console.WriteLine($"Solicitud de publicación: AppId={appId}, Topic={topic}");

            // Verificar si el tema existe
            if (!_topicSubscriptions.TryGetValue(topic, out var subscribers) || subscribers.Count == 0)
            {
                return new ProtocolMessage
                {
                    Operation = OperationType.Publish,
                    Success = false,
                    ErrorMessage = "No hay suscriptores para este tema"
                };
            }

            // Publicar el mensaje a todos los suscriptores
            var subscriberList = subscribers.ToArray();
            foreach (var subscriberId in subscriberList)
            {
                string queueKey = $"{subscriberId}_{topic}";

                // Obtener la cola para este suscriptor y tema
                if (_queues.TryGetValue(queueKey, out var queue))
                {
                    // Añadir el mensaje a la cola
                    queue.Enqueue(content);

                    Console.WriteLine($"Mensaje publicado en la cola de AppId={subscriberId}, Topic={topic}");
                }
            }

            return new ProtocolMessage
            {
                Operation = OperationType.Publish,
                Success = true,
                Topic = topic
            };
        }

        private ProtocolMessage HandleReceive(ProtocolMessage request)
        {
            var appId = request.AppId;
            var topic = request.Topic;

            Console.WriteLine($"Solicitud de recepción: AppId={appId}, Topic={topic}");

            // Crear clave para la cola
            string queueKey = $"{appId}_{topic}";

            // Verificar si el AppId está suscrito al tema
            if (!_topicSubscriptions.TryGetValue(topic, out var subscribers) || !subscribers.Contains(appId))
            {
                return new ProtocolMessage
                {
                    Operation = OperationType.Receive,
                    Success = false,
                    ErrorMessage = "No está suscrito a este tema"
                };
            }

            // Obtener la cola para este AppId y tema
            if (!_queues.TryGetValue(queueKey, out var queue) || queue.IsEmpty)
            {
                return new ProtocolMessage
                {
                    Operation = OperationType.Receive,
                    Success = false,
                    ErrorMessage = "No hay mensajes en la cola"
                };
            }

            // Extraer el mensaje de la cola (FIFO)
            if (queue.Dequeue(out var message))
            {
                return new ProtocolMessage
                {
                    Operation = OperationType.Receive,
                    Success = true,
                    Topic = topic,
                    Content = message
                };
            }

            return new ProtocolMessage
            {
                Operation = OperationType.Receive,
                Success = false,
                ErrorMessage = "No se pudo extraer el mensaje de la cola"
            };
        }
    }
}