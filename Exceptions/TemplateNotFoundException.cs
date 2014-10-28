using System;

namespace BlazeSoft.Net.Web.Exceptions
{
    public class TemplateNotFoundException : Exception
    {
        public TemplateNotFoundException() : base() { }
        public TemplateNotFoundException(string Message) : base(Message) { }
    }
}