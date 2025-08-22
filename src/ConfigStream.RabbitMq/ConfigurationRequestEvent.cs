namespace ConfigStream.RabbitMq
{
    public class ConfigurationRequestEvent
    {
        public string ApplicationName { get; init; }
        public string Key { get; init; }
        public string RequestId { get; set; }
    }
}