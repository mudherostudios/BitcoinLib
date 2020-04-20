namespace BitcoinLib.Parameters
{
    public class FeeOptions
    {
        public decimal feeRate { get; set; }
        public int changePosition { get; set; }

        public FeeOptions() { }
        public FeeOptions(decimal _feeRate, int _changePosition)
        {
            feeRate = _feeRate;
            changePosition = _changePosition;
        }
    }
}
