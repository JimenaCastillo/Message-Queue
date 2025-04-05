namespace MessageQueue.Client
{
    // Representa un tema en la cola de mensajes.

    public class Topic
    {

        // Obtiene el nombre del tema.
        public string Name { get; }

        // Inicializa una nueva instancia de la clase <see cref="Topic"/>.
        public Topic(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del tema no puede estar vacío", nameof(name));

            Name = name;
        }

        // Devuelve una cadena que representa el tema actual.
        public override string ToString()
        {
            return Name;
        }
    }
}
