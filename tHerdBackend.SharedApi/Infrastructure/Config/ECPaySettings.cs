namespace tHerdBackend.SharedApi.Infrastructure.Config
{
    public class ECPaySettings
    {
        public string MerchantID { get; set; }
        public string HashKey { get; set; }
        public string HashIV { get; set; }
        public string PaymentUrl { get; set; }
        public string ReturnUrl { get; set; }
        public string OrderResultUrl { get; set; }
        public bool IsProduction { get; set; }
    }

}
