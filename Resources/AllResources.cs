using System.IO;
using System.Linq;

namespace WemoSwitchAutomation.Resources
{
    public static class AllResources
    {
        public static string SwitchOnRequestContent => GetResourceFileContent(GetFullResourceName("SwitchOnRequest.xml"));
        public static string SwitchOffRequestContent => GetResourceFileContent(GetFullResourceName("SwitchOffRequest.xml"));
        public static string GetSwitchStateRequestContent => GetResourceFileContent(GetFullResourceName("GetSwitchStateRequest.xml"));

        private static string GetResourceFileContent(string resourceName)
        {
            using (TextReader tr = new StreamReader(typeof(AllResources).Assembly.GetManifestResourceStream(resourceName)))
            {
                return tr.ReadToEnd();
            }
        }

        private static string GetFullResourceName(string resourceFileName)
        {
            return typeof(AllResources).Assembly.GetManifestResourceNames().First(r => r.EndsWith("." + resourceFileName));
        }
    }
}