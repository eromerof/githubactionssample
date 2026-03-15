using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PluginsDataverse.UITests
{
    [TestClass]
    public class SampleUITest : PageTest
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            Environment.SetEnvironmentVariable("HEADED", "1");
            Environment.SetEnvironmentVariable("PLAYWRIGHT_SLOW_MO", "500");
        }
    }
}
