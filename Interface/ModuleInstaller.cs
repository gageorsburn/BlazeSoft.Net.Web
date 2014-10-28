using System.Web;

using BlazeSoft.Net.Web.Link;

namespace BlazeSoft.Net.Web.Installer
{
    public class ModuleInstaller
    {
        public ModuleInstaller()
        {
            
        }

        public virtual void Install(bool isChangingVersion, string previousVersionId)
        {

        }

        public virtual void Uninstall(bool isChangingVersion, string newVersionId)
        {

        }
    }
}