using MessageQueue.Client;
using System.Collections.ObjectModel;
using System.Windows;

namespace MessageQueue.GUI
{
    // Lógica de interacción para MainWindow.xaml
    public partial class MainWindow : Window
    {
        private MQClient _client;
        private Guid _appId;
        private ObservableCollection<string> _messages;


        // Inicializa una nueva instancia de la clase <see cref="MainWindow"/>.
        public MainWindow()
        {
            InitializeComponent();

            // Generar un nuevo GUID para la aplicación
            _appId = Guid.NewGuid();
            AppIdTextBox.Text = _appId.ToString();

            // Inicializar la colección de mensajes
            _messages = new ObservableCollection<string>();
            MessagesListBox.ItemsSource = _messages;

            // Crear el cliente
            InitializeClient();
        }

        // Inicializa el cliente MQClient con la IP y el puerto del servidor.
        private void InitializeClient()
        {
            try
            {
                var ip = ServerIpTextBox.Text;
                if (int.TryParse(ServerPortTextBox.Text, out int port))
                {
                    _client = new MQClient(ip, port, _appId);
                    UpdateStatus("Cliente inicializado correctamente");
                }
                else
                {
                    UpdateStatus("Puerto inválido");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al inicializar el cliente: {ex.Message}");
            }
        }

        // Maneja el evento de clic del botón de suscripción.
        private void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var topic = new Topic(TopicTextBox.Text);
                if (_client.Subscribe(topic))
                {
                    UpdateStatus($"Suscrito al tema: {topic}");
                }
                else
                {
                    UpdateStatus($"No se pudo suscribir al tema: {topic}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al suscribirse: {ex.Message}");
            }
        }


        // Maneja el evento de clic del botón de cancelación de suscripción.
        private void UnsubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var topic = new Topic(TopicTextBox.Text);
                if (_client.Unsubscribe(topic))
                {
                    UpdateStatus($"Desuscrito del tema: {topic}");
                }
                else
                {
                    UpdateStatus($"No se pudo desuscribir del tema: {topic}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al desuscribirse: {ex.Message}");
            }
        }

        // Maneja el evento de clic del botón de publicación.
        private void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var topic = new Topic(PublishTopicTextBox.Text);
                var message = new Message(MessageTextBox.Text);

                if (_client.Publish(message, topic))
                {
                    UpdateStatus($"Mensaje publicado en el tema: {topic}");
                    MessageTextBox.Clear();
                }
                else
                {
                    UpdateStatus($"No se pudo publicar el mensaje en el tema: {topic}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al publicar el mensaje: {ex.Message}");
            }
        }

        // Maneja el evento de clic del botón de recepción.
        private void ReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var topic = new Topic(ReceiveTopicTextBox.Text);
                var message = _client.Receive(topic);

                if (message != null)
                {
                    _messages.Insert(0, $"[{topic}] {message}");
                    UpdateStatus($"Mensaje recibido del tema: {topic}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al recibir mensaje: {ex.Message}");
            }
        }

        // Actualiza el estado de la aplicación con el mensaje proporcionado.
        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        // Maneja el evento de cambio de texto en el cuadro de texto del ID de la aplicación.
 
        private void AppIdTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}