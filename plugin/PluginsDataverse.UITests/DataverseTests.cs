using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OtpNet;

namespace PluginsDataverse.UITests
{
    [TestClass]
    public class DataverseTests : PageTest
    {
        private const string OrgUrl = "https://eromerof-cursos.crm4.dynamics.com";
        private const string AppId = "73c10fc1-5220-f111-8342-000d3a67519a";
        private const string EntityName = "erf_tablasparaexportar";
        private const string Username = "hello@enriqueromero.es";
        private const string Password = "tikcuw-dudcic-xAcji3";
        private const string MfaSecretKey = "XGLQYJSD6XDLR57W";
        private const string ValidDni = "12345678Z";
        private const string InvalidDni = "12345678A";

        private async Task LoginAsync()
        {
            await Page.GotoAsync($"{OrgUrl}/main.aspx?appid={AppId}");

            // Email
            await Page.WaitForSelectorAsync("input[name='loginfmt']", new() { Timeout = 30000 });
            await Page.FillAsync("input[name='loginfmt']", Username);
            await Page.ClickAsync("input[type='submit']");

            // Password
            await Page.WaitForSelectorAsync("input[name='passwd']", new() { Timeout = 15000 });
            await Page.FillAsync("input[name='passwd']", Password);
            await Page.ClickAsync("input[type='submit']");

            // MFA: si aparece selector de método, cambiar a código TOTP
            try
            {
                var otherWay = Page.Locator("#signInAnotherWay");
                if (await otherWay.IsVisibleAsync(new() { Timeout = 5000 }))
                {
                    await otherWay.ClickAsync();
                    await Page.Locator("div[data-value='PhoneAppOTP']").ClickAsync();
                }
            }
            catch { }

            // Código TOTP
            await Page.WaitForSelectorAsync("input[name='otc']", new() { Timeout = 15000 });
            var totp = new Totp(Base32Encoding.ToBytes(MfaSecretKey));
            await Page.FillAsync("input[name='otc']", totp.ComputeTotp());
            await Page.ClickAsync("input[type='submit']");

            // "¿Mantener la sesión iniciada?" -> Sí
            try
            {
                await Page.WaitForSelectorAsync("#idSIButton9", new() { Timeout = 5000 });
                await Page.ClickAsync("#idSIButton9");
            }
            catch { }

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60000 });
        }

        private async Task NavigateToNewRecordAsync()
        {
            await Page.GotoAsync(
                $"{OrgUrl}/main.aspx?appid={AppId}&etn={EntityName}&pagetype=entityrecord",
                new PageGotoOptions { Timeout = 60000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60000 });
            await Page.WaitForTimeoutAsync(3000);
        }

        private async Task FillFieldAsync(string fieldName, string value)
        {
            var input = Page.Locator($"input[data-id*='{fieldName}']").First;
            await input.ClickAsync();
            await input.FillAsync(value);
            await Page.Keyboard.PressAsync("Tab");
        }

        private async Task SaveAsync()
        {
            await Page.Keyboard.PressAsync("Control+S");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
            await Page.WaitForTimeoutAsync(3000);
        }

        private async Task DeleteCurrentRecordAsync()
        {
            // Buscar botón Eliminar en barra de comandos
            var deleteBtn = Page.Locator("button[aria-label='Eliminar'], button[aria-label='Delete']").First;
            if (!await deleteBtn.IsVisibleAsync())
            {
                // Buscar en menú de desbordamiento
                var overflowBtn = Page.Locator("button[data-id='OverflowButton'], button[aria-label*='más comandos'], button[aria-label*='More commands']").First;
                await overflowBtn.ClickAsync();
                deleteBtn = Page.Locator("button[aria-label='Eliminar'], button[aria-label='Delete']").First;
            }
            await deleteBtn.ClickAsync();

            // Confirmar eliminación
            await Page.Locator("button:has-text('Eliminar'), button:has-text('Delete')").First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
        }

        [TestMethod]
        public async Task Test1_DNIValido_GuardaSinError()
        {
            await LoginAsync();
            await NavigateToNewRecordAsync();

            await FillFieldAsync("erf_name", "test 1");
            await FillFieldAsync("cd_dni", ValidDni);
            await SaveAsync();

            // Verificar que no hay notificación de error (plugin no lanzó excepción)
            var errorNotif = Page.Locator("[data-id='errorNotification'], [data-id='notificationWrapper'] [role='alert']");
            await Expect(errorNotif).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

            // Verificar que el registro se guardó (URL contiene id del registro)
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("id="), new() { Timeout = 10000 });

            // Limpiar: borrar el registro de test
            await DeleteCurrentRecordAsync();
        }

        [TestMethod]
        public async Task Test2_DNIInvalido_MuestraErrorYNoBorra()
        {
            await LoginAsync();
            await NavigateToNewRecordAsync();

            await FillFieldAsync("erf_name", "test 2");
            await FillFieldAsync("cd_dni", InvalidDni);
            await SaveAsync();

            // Verificar que hay una notificación de error visible
            var errorNotif = Page.Locator("[data-id='notificationWrapper'], [data-id='errorNotification'], [role='alertdialog']");
            await Expect(errorNotif.First).ToBeVisibleAsync(new() { Timeout = 10000 });

            // Verificar que el registro NO se guardó (URL no contiene id)
            Assert.IsFalse(Page.Url.Contains("&id="), "El registro no debería haberse guardado con un DNI inválido");
        }
    }
}
