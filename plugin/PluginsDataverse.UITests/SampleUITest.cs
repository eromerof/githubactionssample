using Microsoft.Playwright;
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

        /// <summary>
        /// Ejemplo de test de UI con Playwright.
        /// Sustituye la URL y los selectores por los de tu aplicación Power Apps.
        /// </summary>
        [TestMethod]
        public async Task PageTitle_ShouldContainExpectedText()
        {
            await Page.GotoAsync("https://www.example.com");

            await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Example"));
        }
    }
}
