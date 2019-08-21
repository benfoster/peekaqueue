using System;

namespace Peekaqueue
{
    public class UnableToConnectToSqsException : Exception
    {
        public UnableToConnectToSqsException(Exception exception): base("Unable to connect to SQS", exception)
        { }
    }
}
