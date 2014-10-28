using System;

namespace BlazeSoft.Net.Web.Exceptions
{
    public class ModuleNotFoundException : Exception
    {
        public ModuleNotFoundException(string ModuleID) : base(string.Format("Unable to load module {0}", ModuleID))
        {
            
        }
    }
}