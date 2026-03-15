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
            await Page.GotoAsync($"{TestConfig.OrgUrl}/main.aspx?appid={TestConfig.AppId}");

            // Email
            await Page.WaitForSelectorAsync("input[name='loginfmt']", new() { Timeout = 30000 });
            await Page.FillAsync("input[name='loginfmt']", TestConfig.Username);
            await Page.ClickAsync("input[type='submit']");

            // Password
            await Page.WaitForSelectorAsync("input[name='passwd']", new() { Timeout = 15000 });
            await Page.FillAsync("input[name='passwd']", TestConfig.Password);
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
            await Page.FillAsync("input[name='otc']", GenerateTotp(TestConfig.MfaSecretKey));
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
                $"{TestConfig.OrgUrl}/main.aspx?appid={TestConfig.AppId}&pagetype=entitylist&etn={TestConfig.EntityName}",
                new PageGotoOptions { Timeout = 60000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60000 });
            await Page.WaitForTimeoutAsync(3000);

            // Clic en "+ Nuevo" — buscar por texto visible
            var newBtn = Page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Nuevo" }).First;
            await newBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
            await newBtn.ClickAsync();

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
            var deleteBtn = Page.Locator("button[aria-label='Eliminar'], button[aria-label='Delete']").First;
            if (!await deleteBtn.IsVisibleAsync())
            {
                var overflowBtn = Page.Locator("button[data-id='OverflowButton'], button[aria-label*='más comandos'], button[aria-label*='More commands']").First;
                await overflowBtn.ClickAsync();
                deleteBtn = Page.Locator("button[aria-label='Eliminar'], button[aria-label='Delete']").First;
            }
            await deleteBtn.ClickAsync();

            await Page.Locator("button:has-text('Eliminar'), button:has-text('Delete')").First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
        }

        [TestMethod]
        public async Task Test1_DNIValido_GuardaSinError()
        {
            await LoginAsync();
            await NavigateToNewRecordAsync();

            await FillFieldAsync("erf_name", "test 1");
            await FillFieldAsync("cd_dni", TestConfig.ValidDni);
            await SaveAsync();

            // Verificar que no hay notificación de error
            var errorNotif = Page.Locator("[data-id='errorNotification'], [data-id='notificationWrapper'] [role='alert']");
            await Expect(errorNotif).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

            // Verificar que el registro se guardó (URL contiene id del registro)
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("id="), new() { Timeout = 10000 });

            // Limpiar: borrar el registro de test
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

            // Verificar que hay una notificación de error visible
            var errorNotif = Page.Locator("[data-id='notificationWrapper'], [data-id='errorNotification'], [role='alertdialog']");
            await Expect(errorNotif.First).ToBeVisibleAsync(new() { Timeout = 10000 });

            // Verificar que el registro NO se guardó (URL no contiene id)
            Assert.IsFalse(Page.Url.Contains("&id="), "El registro no debería haberse guardado con un DNI inválido");
        }
    }
}
