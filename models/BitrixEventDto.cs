namespace BitrixSheets.models
{
    public class BitrixEventDto
    {
        public string Event { get; set; }
        public BitrixEventData Data { get; set; }
    }

    public class BitrixEventData
    {
        public BitrixFields FIELDS { get; set; }
    }

    public class BitrixFields
    {
        public int ID { get; set; }
    }

    // Модель для ответа от API Битрикс24 (crm.lead.get)
    public class BitrixLeadResponse
    {
        public BitrixLeadResult result { get; set; }
    }

    public class BitrixLeadResult
    {
        public string ID { get; set; }
        public string TITLE { get; set; }
        public string NAME { get; set; }
        public string LAST_NAME { get; set; }
        public string STATUS_ID { get; set; }
        public string COMMENTS { get; set; }
        public string DATE_CLOSED { get; set; }
        // Добавьте другие поля по необходимости
    }
}
