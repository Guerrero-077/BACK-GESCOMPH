using Business.Interfaces.PDF;
using Entity.DTOs.Implements.Business.Contract;
using Microsoft.Playwright;
using System.Text;
using System.Globalization;
using Templates.Templates;

namespace Business.Services.Utilities.PDF
{
    /// <summary>
    /// Servicio para generar contratos PDF usando Playwright.
    /// Optimizado para reutilizar instancias globales de navegador y mejorar rendimiento.
    /// </summary>
    public class ContractPdfService : IContractPdfGeneratorService
    {
        private static readonly SemaphoreSlim _initLock = new(1, 1);
        private static IPlaywright? _playwright;
        private static IBrowser? _browser;

        private const string ClausesPlaceholder = "{{CLAUSES}}";

        /// <summary>
        /// Genera un contrato PDF a partir de la información del contrato.
        /// Reutiliza el navegador, renderiza la plantilla HTML y exporta a PDF.
        /// </summary>
        /// <param name="contract">Contrato con datos de arrendatario, cláusulas, montos y fechas.</param>
        /// <returns>Archivo PDF en bytes.</returns>
        public async Task<byte[]> GeneratePdfAsync(ContractSelectDto contract)
        {
            var template = ContractTemplate.Html;
            var html = BuildHtml(template, contract);

            await EnsureBrowserAsync();

            var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = null,
                JavaScriptEnabled = false,
                BypassCSP = true
            });

            try
            {
                await context.RouteAsync("**/*", route =>
                {
                    var url = route.Request.Url;
                    if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        return route.AbortAsync();

                    return route.ContinueAsync();
                });

                var page = await context.NewPageAsync();
                await page.EmulateMediaAsync(new() { Media = Media.Print });

                await page.SetContentAsync(
                    html,
                    new PageSetContentOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 2000
                    });

                var pdfBytes = await page.PdfAsync(new PagePdfOptions
                {
                    Format = "Letter",
                    PrintBackground = true,
                    PreferCSSPageSize = true,
                    DisplayHeaderFooter = true,
                    HeaderTemplate = "<div></div>",
                    FooterTemplate =
                        "<div style=\"font-size:10px;width:100%;text-align:center;color:#555;\">" +
                        "Página <span class=\"pageNumber\"></span> de <span class=\"totalPages\"></span>" +
                        "</div>",
                    Margin = new()
                    {
                        Top = "25mm",
                        Bottom = "25mm",
                        Left = "25mm",
                        Right = "25mm"
                    }
                });

                return pdfBytes;
            }
            finally
            {
                await context.CloseAsync();
            }
        }

        /// <summary>
        /// Precalienta Playwright y el navegador para reducir la latencia en la primera generación de PDF.
        /// </summary>
        public Task WarmupAsync() => EnsureBrowserAsync();

        /// <summary>
        /// Inicializa Playwright y el navegador Chromium si no existen aún.
        /// Implementa control de concurrencia para evitar inicializaciones múltiples.
        /// </summary>
        private static async Task EnsureBrowserAsync()
        {
            if (_browser is not null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_browser is not null) return;

                _playwright ??= await Playwright.CreateAsync();
                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu",
                        "--disable-extensions",
                        "--blink-settings=imagesEnabled=false"
                    }
                };

                _browser = await _playwright.Chromium.LaunchAsync(launchOptions);
            }
            catch
            {
                _browser = null;
                _playwright = null;
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Construye el HTML del contrato reemplazando placeholders de la plantilla.
        /// Incluye renderizado de cláusulas y codificación HTML básica.
        /// </summary>
        private static string BuildHtml(string template, ContractSelectDto c)
        {
            var sb = new StringBuilder(template);

            sb.Replace("@Model.FullName", HtmlEncode(c.FullName));
            sb.Replace("@Model.Document", HtmlEncode(c.Document));
            sb.Replace("@Model.Phone", HtmlEncode(c.Phone));
            sb.Replace("@Model.Email", HtmlEncode(c.Email ?? string.Empty));
            sb.Replace("@Model.StartDate.ToString(\"dd/MM/yyyy\")", c.StartDate.ToString("dd/MM/yyyy"));
            sb.Replace("@Model.EndDate.ToString(\"dd/MM/yyyy\")", c.EndDate.ToString("dd/MM/yyyy"));

            sb.Replace("@Model.ContractNumber", c.Id.ToString());
            sb.Replace("@Model.ContractYear", c.StartDate.Year.ToString());

            var durationMonths = ((c.EndDate.Year - c.StartDate.Year) * 12) + (c.EndDate.Month - c.StartDate.Month);
            if (durationMonths < 0) durationMonths = 0;
            sb.Replace("@Model.DurationMonths", durationMonths.ToString());

            var esCO = new CultureInfo("es-CO");
            string money = c.TotalBaseRentAgreed.ToString("N0", esCO);
            string uvtQty = c.TotalUvtQtyAgreed.ToString("0.##", CultureInfo.InvariantCulture);
            sb.Replace("@Model.MonthlyRentAmount", money);

            string moneyWords;
            try { moneyWords = NumberToSpanishWords(c.TotalBaseRentAgreed); }
            catch { moneyWords = money; }
            sb.Replace("@Model.MonthlyRentAmountWords", moneyWords);
            sb.Replace("@Model.UVTValue", uvtQty);

            sb.Replace("@Model.Address", HtmlEncode(c.Address ?? string.Empty));

            var p = c.PremisesLeased?.FirstOrDefault();
            if (p != null)
            {
                sb.Replace("@Model.PremisesLeased[0].EstablishmentName", HtmlEncode(p.EstablishmentName));
                sb.Replace("@Model.PremisesLeased[0].Address", HtmlEncode(p.Address));
                sb.Replace("@Model.PremisesLeased[0].AreaM2", p.AreaM2.ToString("0.##"));
                sb.Replace("@Model.PremisesLeased[0].PlazaName", HtmlEncode(p.PlazaName));
            }

            var clausesHtml = new StringBuilder();
            if (c.Clauses is not null)
            {
                foreach (var clause in c.Clauses)
                {
                    if (!string.IsNullOrWhiteSpace(clause?.Description))
                        clausesHtml.AppendLine($"<li>{HtmlEncode(clause.Description)}</li>");
                }
            }

            sb.Replace(ClausesPlaceholder, clausesHtml.ToString());

            var logoB64 = TryLoadLogoBase64();
            sb.Replace("{{LOGO_BASE64}}", logoB64 ?? string.Empty);

            return sb.ToString();
        }

        /// <summary>
        /// Codifica texto para uso seguro en HTML (escape de caracteres especiales).
        /// </summary>
        private static string HtmlEncode(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        /// <summary>
        /// Busca y convierte el logo en Base64 desde las rutas conocidas.
        /// </summary>
        private static string? TryLoadLogoBase64()
        {
            try
            {
                var baseDirs = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
                foreach (var baseDir in baseDirs)
                {
                    var candidates = new[]
                    {
                        Path.Combine(baseDir, "wwwroot", "logo.png"),
                        Path.Combine(baseDir, "wwwroot", "img", "logo.png"),
                        Path.Combine(baseDir, "public", "logo.png"),
                        Path.Combine(baseDir, "public", "img", "logo.png"),
                    };

                    foreach (var p in candidates)
                    {
                        if (File.Exists(p))
                        {
                            var bytes = File.ReadAllBytes(p);
                            return Convert.ToBase64String(bytes);
                        }
                    }
                }
            }
            catch
            {
                // El logo es opcional, se ignora el error.
            }
            return null;
        }

        /// <summary>
        /// Convierte un número decimal a su representación en palabras en español.
        /// </summary>
        private static string NumberToSpanishWords(decimal value)
        {
            var n = (long)Math.Round(value, 0, MidpointRounding.AwayFromZero);
            if (n == 0) return "CERO";
            if (n < 0) return "MENOS " + NumberToSpanishWords(-n);

            var parts = new List<string>();
            void Add(string s) { if (!string.IsNullOrWhiteSpace(s)) parts.Add(s); }

            long billions = n / 1_000_000_000; n %= 1_000_000_000;
            long millions = n / 1_000_000; n %= 1_000_000;
            long thousands = n / 1_000; n %= 1_000;
            long hundreds = n;

            if (billions > 0)
                Add((billions == 1 ? "UN" : NumberToSpanishWords(billions)) + " MIL MILLONES");
            if (millions > 0)
                Add((millions == 1 ? "UN MILLÓN" : NumberToSpanishWords(millions) + " MILLONES"));
            if (thousands > 0)
                Add(thousands == 1 ? "MIL" : NumberToSpanishWords(thousands) + " MIL");
            if (hundreds > 0)
                Add(ThreeDigitsToSpanish(hundreds));

            return string.Join(" ", parts).Trim().Replace("  ", " ");
        }

        /// <summary>
        /// Convierte números de tres dígitos a texto en español (100-999).
        /// </summary>
        private static string ThreeDigitsToSpanish(long n)
        {
            string[] unidades = { "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
            string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            if (n == 100) return "CIEN";
            if (n < 20) return unidades[n];

            var c = n / 100; n %= 100;
            var d = n / 10; n %= 10;

            var sb = new StringBuilder();
            if (c > 0) sb.Append(centenas[c] + " ");

            if (d == 0)
            {
                if (n > 0) sb.Append(unidades[n]);
            }
            else if (d == 1)
            {
                sb.Append(unidades[10 + n]);
            }
            else if (d == 2)
            {
                if (n == 0) sb.Append("VEINTE");
                else sb.Append("VEINTI" + ToVeinti(unidades[n]));
            }
            else
            {
                sb.Append(decenas[d]);
                if (n > 0) sb.Append(" Y " + unidades[n]);
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Ajusta terminaciones en palabras del rango "veinti..." (ej. veintiuno, veintidós, etc.)
        /// </summary>
        private static string ToVeinti(string unidad) =>
            unidad.ToLower() switch
            {
                "uno" => "UNO",
                "dos" => "DOS",
                "tres" => "TRES",
                "cuatro" => "CUATRO",
                "cinco" => "CINCO",
                "seis" => "SEIS",
                "siete" => "SIETE",
                "ocho" => "OCHO",
                "nueve" => "NUEVE",
                _ => unidad.ToUpper()
            };
    }
}
