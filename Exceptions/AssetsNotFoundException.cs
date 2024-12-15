namespace InvestmentManager.Exceptions
{
    public class AssetsNotFoundException : Exception
    {
        public AssetsNotFoundException()
        {
        }

        public AssetsNotFoundException(string message)
            : base(message)
        {
        }

        public AssetsNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
