using MessageQueue.Client;
using System.Collections.ObjectModel;
using System.Windows;

namespace MessageQueue.GUI
{
    public partial class MainWindow : Window
    {
        private MQClient _client;
        private Guid _appId;
        private ObservableCollection<string> _messages;

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
                    UpdateStatus($"No se pudo desusbcribir del tema: {topic}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al desuscribirse: {ex.Message}");
            }
        }

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

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        private void AppIdTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}