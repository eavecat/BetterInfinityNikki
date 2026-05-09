namespace BetterInfinityNikki.GameTask.Common.Exceptions; // TODO: change this namespace to BetterGenshinImpact.GameTask.Common.Exception

public class RetryNoCountException : Exception
{
    public RetryNoCountException() : base()
    {
    }

    public RetryNoCountException(string message) : base(message)
    {
    }
}
