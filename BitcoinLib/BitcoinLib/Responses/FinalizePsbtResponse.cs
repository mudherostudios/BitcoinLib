namespace BitcoinLib.Responses
{
    public class FinalizePsbtResponse
    {
        public string Psbt { get; set; }
        public string Hex { get; set; }
        public bool Complete { get; set; }
    }
}
