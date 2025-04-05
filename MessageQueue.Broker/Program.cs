namespace MessageQueue.Broker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando MQBroker...");
            var broker = new MQBroker();
            await broker.Start();
        }
    }
}