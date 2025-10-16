using Business.Interfaces.PDF;
using Entity.DTOs.Implements.Business.Contract;
using Microsoft.Playwright;
using System.Text;
using System.Globalization;
using Templates.Templates;

namespace Business.Services.Utilities.PDF
{
    /// <summary>
    /// Generador de PDF basado en Playwright con reutilización de Browser.
    /// - Reutiliza IPlaywright e IBrowser (lazy, thread-safe)
    /// - Crea IBrowserContext e IPage por request (thread-safe)
    /// - Optimiza reemplazos y tiempos de espera
    /// </summary>
    public class ContractPdfService : IContractPdfGeneratorService
    {
        // Lazy singletons para evitar condiciones de carrera en arranque
        private static readonly SemaphoreSlim _initLock = new(1, 1);
        private static IPlaywright? _playwright;
        private static IBrowser? _browser;

        // Marcador simple en plantilla para cláusulas
        private const string ClausesPlaceholder = "{{CLAUSES}}";

        public async Task<byte[]> GeneratePdfAsync(ContractSelectDto contract)
        {
            // 1) Preparar HTML a partir de plantilla
            var template = ContractTemplate.Html;
            var html = BuildHtml(template, contract);

            // 2) Asegurar que Playwright y Browser están inicializados una sola vez
            await EnsureBrowserAsync();

            // 3) Usar un contexto/página por request (thread-safe)
            //    Contexto limpio = sin fugas de estado/cookies/almacenamiento.
            var context = await _browser!.NewContextAsync(new()
            {
                // Viewport null => tamaño "fit to page" para PDF,
                // no imprescindible, pero ayuda a evitar reflow extraño.
                ViewportSize = null,
                JavaScriptEnabled = false, // desactiva JS para acelerar si no se usa
                BypassCSP = true
            });

            try
            {
                // Bloquear solo recursos externos http/https (permitir about:blank, data:)
                await context.RouteAsync("**/*", route =>
                {
                    var url = route.Request.Url;
                    if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        return route.AbortAsync();
                    }
                    return route.ContinueAsync();
                });

                var page = await context.NewPageAsync();

                // Emular media "print" para respetar estilos de impresión
                await page.EmulateMediaAsync(new() { Media = Media.Print });

                // 4) Cargar HTML y esperar a estar ocioso en red.
                //    BaseURL opcional si usas rutas relativas a recursos (css/img).
                await page.SetContentAsync(
                    html,
                    new PageSetContentOptions
                    {
                        // Para contenido estático embebido, DOMContentLoaded es suficiente
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 2_000
                    });

                // 5) Exportar PDF
                var pdfBytes = await page.PdfAsync(new PagePdfOptions
                {
                    Format = "Letter",
                    PrintBackground = true,
                    PreferCSSPageSize = true, // respeta @page css size si lo defines
                    DisplayHeaderFooter = true,
                    HeaderTemplate = "<div></div>", // vacío pero necesario si se activa
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

        public Task WarmupAsync()
        {
            return EnsureBrowserAsync();
        }

        /// <summary>
        /// Inicializa IPlaywright e IBrowser una sola vez. Thread-safe y tolerante a caídas.
        /// </summary>
        private static async Task EnsureBrowserAsync()
        {
            if (_browser is not null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_browser is not null) return;

                _playwright ??= await Playwright.CreateAsync();

                // Flags útiles en contenedores Linux (ajústalos por entorno)
                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        // Comenta si NO estás en contenedor endurecido
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
                // Si algo falló, forzar reintento en la próxima llamada
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
        /// Construye el HTML final: reemplazos directos + renderizado de cláusulas.
        /// Evita usos innecesarios de Regex salvo para el bloque foreach.
        /// </summary>
        private static string BuildHtml(string template, ContractSelectDto c)
        {
            var sb = new StringBuilder(template);

            // Reemplazos simples (usa valores seguros)
            sb.Replace("@Model.FullName", HtmlEncode(c.FullName));
            sb.Replace("@Model.Document", HtmlEncode(c.Document));
            sb.Replace("@Model.Phone", HtmlEncode(c.Phone));
            sb.Replace("@Model.Email", HtmlEncode(c.Email ?? string.Empty));
            sb.Replace("@Model.StartDate.ToString(\"dd/MM/yyyy\")", c.StartDate.ToString("dd/MM/yyyy"));
            sb.Replace("@Model.EndDate.ToString(\"dd/MM/yyyy\")", c.EndDate.ToString("dd/MM/yyyy"));

            // Datos derivados del contrato
            sb.Replace("@Model.ContractNumber", c.Id.ToString());
            sb.Replace("@Model.ContractYear", c.StartDate.Year.ToString());

            // Cálculo aproximado de duración en meses (diferencia año-mes)
            var durationMonths = ((c.EndDate.Year - c.StartDate.Year) * 12) + (c.EndDate.Month - c.StartDate.Month);
            if (durationMonths < 0) durationMonths = 0; // seguridad
            sb.Replace("@Model.DurationMonths", durationMonths.ToString());

            // Montos: total base pactado y cantidad UVT pactada
            var esCO = new CultureInfo("es-CO");
            string money = c.TotalBaseRentAgreed.ToString("N0", esCO);
            string uvtQty = c.TotalUvtQtyAgreed.ToString("0.##", CultureInfo.InvariantCulture);
            sb.Replace("@Model.MonthlyRentAmount", money);
            // Si no hay conversión a letras, usar mismo valor numérico para evitar placeholders sin reemplazar
            // Monto en letras (ES). Si falla, usar numérico como respaldo.
            string moneyWords;
            try { moneyWords = NumberToSpanishWords(c.TotalBaseRentAgreed); }
            catch { moneyWords = money; }
            sb.Replace("@Model.MonthlyRentAmountWords", moneyWords);
            sb.Replace("@Model.UVTValue", uvtQty);

            // Dirección del arrendatario no está en el DTO; evitar dejar el placeholder sin reemplazo
            // Dirección del arrendatario
            sb.Replace("@Model.Address", HtmlEncode(c.Address ?? string.Empty));


            var p = c.PremisesLeased?.FirstOrDefault();
            if (p != null)
            {
                sb.Replace("@Model.PremisesLeased[0].EstablishmentName", HtmlEncode(p.EstablishmentName));
                sb.Replace("@Model.PremisesLeased[0].Address", HtmlEncode(p.Address));
                sb.Replace("@Model.PremisesLeased[0].AreaM2", p.AreaM2.ToString("0.##"));
                sb.Replace("@Model.PremisesLeased[0].PlazaName", HtmlEncode(p.PlazaName));
            }

            // Renderizar cláusulas
            var clausesHtml = new StringBuilder();
            if (c.Clauses is not null)
            {
                foreach (var clause in c.Clauses)
                {
                    if (!string.IsNullOrWhiteSpace(clause?.Description))
                        clausesHtml.AppendLine($"<li>{HtmlEncode(clause.Description)}</li>");
                }
            }

            // Reemplazo directo del placeholder de cláusulas
            sb.Replace(ClausesPlaceholder, clausesHtml.ToString());

            // Logo opcional: intentar cargar desde wwwroot/public
            var logoB64 = TryLoadLogoBase64();
            sb.Replace("{{LOGO_BASE64}}", logoB64 ?? string.Empty);
            return sb.ToString();
        }

        /// <summary>
        /// Muy básico para evitar romper el HTML con datos de usuario.
        /// (Si ya controlas el origen, puedes omitirlo.)
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
                // ignore: logo es opcional
            }
            return null;
        }

        // Conversión simple de números a palabras en español (enteros, hasta miles de millones)
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

        private static string ToVeinti(string unidad)
        {
            // Maneja contracciones habituales: VEINTIÚN antes de sustantivo, aquí dejamos VEINTIUNO genérico
            return unidad.ToLower() switch
            {
                "uno" => "UNO" ,
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
}
