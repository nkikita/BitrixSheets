namespace BitrixSheets.models
{
    public class BitrixWebhookDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Comment { get; set; }
        public string Base { get; set; }
        public DateTime DateClosed { get; set; }
    }
}
