using System;

namespace BlazeSoft.Net.Web.Exceptions
{
    public class AuthorizationLevelException : Exception
    {
        public AuthorizationLevelException() : base("You are required to sign in to access this page.") { }
        public AuthorizationLevelException(string Message) : base(Message) { }
    }
}