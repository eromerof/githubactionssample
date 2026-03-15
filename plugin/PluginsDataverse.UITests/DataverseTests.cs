using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;

namespace PluginsDataverse.UITests
{
    [TestClass]
    public class DataverseTests : PageTest
    {
        private static string GenerateTotp(string base32Secret)
        {
            var secret = Base32Decode(base32Secret.ToUpper());
            var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
            var counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

            using var hmac = new HMACSHA1(secret);
            var hash = hmac.ComputeHash(counterBytes);
            var offset = hash[^1] & 0x0F;
            var code = ((hash[offset] & 0x7F) << 24)
                     | ((hash[offset + 1] & 0xFF) << 16)
                     | ((hash[offset + 2] & 0xFF) << 8)
                     | (hash[offset + 3] & 0xFF);
            return (code % 1_000_000).ToString("D6");
        }

        private static byte[] Base32Decode(string base32)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new List<byte>();
            int buffer = 0, bitsLeft = 0;
            foreach (var c in base32)
            {
                if (c == '=') break;
                var idx = alphabet.IndexOf(c);
                if (idx < 0) continue;
                buffer = (buffer << 5) | idx;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    result.Add((byte)(buffer >> (bitsLeft - 8)));
                    bitsLeft -= 8;
                }
            }
            return result.ToArray();
        }

        private async Task LoginAsync()
        {
            try { await Page.GotoAsync($"{TestConfig.OrgUrl}/main.aspx?appid={TestConfig.AppId}"); }
            catch (Exception ex) { throw new Exception($"[Login] GotoAsync falló. URL: {Page.Url}", ex); }

            try { await Page.WaitForSelectorAsync("input[name='loginfmt']", new() { Timeout = 30000 }); }
            catch (Exception ex) { throw new Exception($"[Login] Email input no encontrado. URL: {Page.Url}", ex); }

            await Page.FillAsync("input[name='loginfmt']", TestConfig.Username);
            await Page.ClickAsync("input[type='submit']");

            try { await Page.WaitForSelectorAsync("input[name='passwd']", new() { Timeout = 15000 }); }
            catch (Exception ex) { throw new Exception($"[Login] Password input no encontrado. URL: {Page.Url}", ex); }

            await Page.FillAsync("input[name='passwd']", TestConfig.Password);
            await Page.ClickAsync("input[type='submit']");

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

            try { await Page.WaitForSelectorAsync("input[name='otc']", new() { Timeout = 15000 }); }
            catch (Exception ex) { throw new Exception($"[Login] TOTP input no encontrado. URL: {Page.Url}", ex); }

            await Page.FillAsync("input[name='otc']", GenerateTotp(TestConfig.MfaSecretKey));
            await Page.ClickAsync("input[type='submit']");

            try
            {
                await Page.WaitForSelectorAsync("#idSIButton9", new() { Timeout = 5000 });
                await Page.ClickAsync("#idSIButton9");
            }
            catch { }

            try { await Page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 60000 }); }
            catch (Exception ex) { throw new Exception($"[Login] NetworkIdle no alcanzado. URL: {Page.Url}", ex); }
        }

        private async Task NavigateToNewRecordAsync()
        {
            try
            {
                await Page.GotoAsync(
                    $"{TestConfig.OrgUrl}/main.aspx?appid={TestConfig.AppId}&pagetype=entitylist&etn={TestConfig.EntityName}",
                    new PageGotoOptions { Timeout = 60000 });
                await Page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 60000 });
            }
            catch (Exception ex) { throw new Exception($"[Navigate] Navegación a entitylist falló. URL: {Page.Url}", ex); }

            // Esperar a que Xrm esté completamente inicializado
            try
            {
                await Page.WaitForFunctionAsync(
                    "() => typeof Xrm !== 'undefined' && typeof Xrm.Navigation !== 'undefined'",
                    options: new PageWaitForFunctionOptions { Timeout = 30000 });
            }
            catch (Exception ex) { throw new Exception($"[Navigate] Xrm.Navigation no disponible tras 30s. URL: {Page.Url}", ex); }

            // Navegar al formulario de nuevo registro via Xrm
            try
            {
                await Page.EvaluateAsync(@"
                    Xrm.Navigation.navigateTo({
                        pageType: 'entityrecord',
                        entityName: 'erf_tablasparaexportar'
                    });
                ");
            }
            catch (Exception ex) { throw new Exception($"[Navigate] Xrm.Navigation.navigateTo falló. URL: {Page.Url}", ex); }

            // Esperar a que el formulario cargue
            try
            {
                await Page.WaitForSelectorAsync(
                    "input[data-id='erf_name.fieldControl-text-box-text']",
                    new() { Timeout = 30000 });
            }
            catch (Exception ex)
            {
                var title = await Page.TitleAsync();
                var dataIds = await Page.EvaluateAsync<string>(
                    "JSON.stringify(Array.from(document.querySelectorAll('[data-id]')).slice(0,20).map(e => e.getAttribute('data-id')))");
                throw new Exception(
                    $"[Navigate] Campo erf_name no apareció. URL: {Page.Url}. Title: {title}. DataIds: {dataIds}", ex);
            }
        }

        private async Task FillFieldAsync(string fieldName, string value)
        {
            var input = Page.Locator($"input[data-id='{fieldName}.fieldControl-text-box-text']");
            try { await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 }); }
            catch (Exception ex) { throw new Exception($"[Fill] Campo '{fieldName}' no visible. URL: {Page.Url}", ex); }

            await input.ClickAsync();
            await input.FillAsync(value);
            await Page.Keyboard.PressAsync("Tab");
        }

        private async Task SaveAsync()
        {
            var saveBtn = Page.Locator("button[aria-label='Guardar (CTRL+S)']");
            try { await saveBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 }); }
            catch (Exception ex) { throw new Exception($"[Save] Botón Guardar no visible. URL: {Page.Url}", ex); }

            await saveBtn.ClickAsync(new() { Force = true });

            try { await Page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 30000 }); }
            catch (Exception ex) { throw new Exception($"[Save] NetworkIdle no alcanzado tras guardar. URL: {Page.Url}", ex); }

            await Page.WaitForTimeoutAsync(3000);
        }

        private async Task DeleteCurrentRecordAsync()
        {
            var deleteBtn = Page.Locator("button[aria-label='Eliminar'], button[aria-label='Delete']").First;
            if (!await deleteBtn.IsVisibleAsync())
            {
                var overflowBtn = Page.Locator("button[data-id='OverflowButton'], button[aria-label*='más comandos'], button[aria-label*='More commands']").First;
                await overflowBtn.ClickAsync();
                deleteBtn = Page.Locator("button[aria-label='Eliminar'], button[aria-label='Delete']").First;
            }
            await deleteBtn.ClickAsync();
            await Page.Locator("button:has-text('Eliminar'), button:has-text('Delete')").First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 30000 });
        }

        [TestMethod]
        public async Task Test1_DNIValido_GuardaSinError()
        {
            await LoginAsync();
            await NavigateToNewRecordAsync();

            await FillFieldAsync("erf_name", "test 1");
            await FillFieldAsync("cd_dni", TestConfig.ValidDni);
            await SaveAsync();

            var errorNotif = Page.Locator("[data-id='errorNotification'], [data-id='notificationWrapper'] [role='alert']");
            await Expect(errorNotif).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("id="), new() { Timeout = 10000 });

            await DeleteCurrentRecordAsync();
        }

        [TestMethod]
        public async Task Test2_DNIInvalido_MuestraErrorYNoGuarda()
        {
            await LoginAsync();
            await NavigateToNewRecordAsync();

            await FillFieldAsync("erf_name", "test 2");
            await FillFieldAsync("cd_dni", TestConfig.InvalidDni);
            await SaveAsync();

            // El plugin lanza un diálogo "Error de proceso empresarial"
            var errorDialog = Page.Locator("[role='dialog'], [role='alertdialog']")
                .Filter(new LocatorFilterOptions { HasText = "Error de proceso empresarial" });

            try { await Expect(errorDialog).ToBeVisibleAsync(new() { Timeout = 10000 }); }
            catch (Exception ex) { throw new Exception($"[Test2] Diálogo de error del plugin no apareció. URL: {Page.Url}", ex); }

            // Cerrar el diálogo
            await Page.Locator("button:has-text('Aceptar')").ClickAsync();

            Assert.IsFalse(Page.Url.Contains("&id="), "El registro no debería haberse guardado con DNI inválido");
        }
    }
}
