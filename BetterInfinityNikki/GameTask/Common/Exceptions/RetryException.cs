namespace BetterInfinityNikki.GameTask.Common.Exceptions; // TODO: change this namespace to BetterGenshinImpact.GameTask.Common.Exception

public class RetryException : Exception
{
    public RetryException() : base()
    {
    }

    public RetryException(string message) : base(message)
    {
    }
}
