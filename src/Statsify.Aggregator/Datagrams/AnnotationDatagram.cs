namespace Statsify.Aggregator.Datagrams
{
    public class AnnotationDatagram : Datagram
    {
        public string Title { get; private set; }

        public string Message { get; private set; }

        public AnnotationDatagram(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}