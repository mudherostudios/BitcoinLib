namespace BitcoinLib.Responses
{
    public class ErrorResponse
    {
        public string Error { get; set; }

        public ErrorResponse() { }
        public ErrorResponse(string error)
        {
            Error = error;
        }
    }
}
