using Microsoft.Dynamics365.UIAutomation.Api.UCI;
using Microsoft.Dynamics365.UIAutomation.Browser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace PluginsDataverse.UITests
{
    [TestClass]
    public class DataverseTests
    {
        private XrmApp _xrmApp = null!;
        private WebClient _webClient = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            var options = new BrowserOptions
            {
                BrowserType = BrowserType.Chrome,
                PrivateMode = false,
                FireEvents = false,
                Headless = false,
                DefaultThinkTime = 2000
            };

            _webClient = new WebClient(options);
            _xrmApp = new XrmApp(_webClient);

            _xrmApp.OnlineLogin.Login(
                new Uri(TestConfig.OrgUrl),
                TestConfig.Username.ToSecureString(),
                TestConfig.Password.ToSecureString(),
                TestConfig.MfaSecretKey.ToSecureString()
            );
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _xrmApp?.Dispose();
        }

        private void NavigateToNewRecord()
        {
            Driver.Navigate().GoToUrl(
                $"{TestConfig.OrgUrl}/main.aspx?appid={TestConfig.AppId}&pagetype=entitylist&etn={TestConfig.EntityName}");
            _xrmApp.CommandBar.ClickCommand("Nuevo");
        }

        private IWebDriver Driver => _webClient.Browser.Driver;

        private bool HasErrorNotification()
        {
            var elements = Driver.FindElements(By.CssSelector(
                "[data-id='notificationWrapper'], [data-id='errorNotification'], [role='alertdialog']"));
            return elements.Any(e => e.Displayed);
        }

        [TestMethod]
        public void Test1_DNIValido_GuardaSinError()
        {
            NavigateToNewRecord();

            _xrmApp.Entity.SetValue("erf_name", "test 1");
            _xrmApp.Entity.SetValue("cd_dni", TestConfig.ValidDni);
            _xrmApp.Entity.Save();

            // Verificar que el registro se guardó (URL contiene id del registro)
            Assert.IsTrue(Driver.Url.Contains("&id="),
                "El registro debería haberse guardado correctamente con DNI válido");

            // Verificar que no hay error de plugin
            Assert.IsFalse(HasErrorNotification(),
                "No debería haber errores al guardar con DNI válido");

            // Limpiar: borrar el registro de test
            _xrmApp.CommandBar.ClickCommand("Eliminar");
            _xrmApp.Dialogs.ConfirmationDialog(true);
        }

        [TestMethod]
        public void Test2_DNIInvalido_MuestraErrorYNoGuarda()
        {
            NavigateToNewRecord();

            _xrmApp.Entity.SetValue("erf_name", "test 2");
            _xrmApp.Entity.SetValue("cd_dni", TestConfig.InvalidDni);
            _xrmApp.Entity.Save();

            // Verificar que hay error de plugin visible
            Assert.IsTrue(HasErrorNotification(),
                "Debería haber un error de validación con DNI inválido");

            // Verificar que el registro NO se guardó (URL no contiene id)
            Assert.IsFalse(Driver.Url.Contains("&id="),
                "El registro no debería haberse guardado con DNI inválido");
        }
    }
}
