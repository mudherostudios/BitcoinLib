namespace BitcoinLib.Parameters
{
    public class NameOperation
    {
        public string op { get; set; }
        public string name { get; set; }
        public string value { get; set; }

        public NameOperation() { }
        public NameOperation(string _operation, string _name, string _value)
        {
            op = _operation;
            name = _name;
            value = _value;
        }
    }
}
