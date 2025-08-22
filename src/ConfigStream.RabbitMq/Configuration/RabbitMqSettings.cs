namespace ConfigStream.RabbitMq.Configuration
{
    public class RabbitMqSettings
    {
        public string Host { get; set; } =  "localhost";
        public ushort Port { get; set; } = 5672;
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
    }
}