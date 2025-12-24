// Services/PreviredScraperService.cs
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;
using Indicadores.Models;

namespace Indicadores.Services
{
    public class PreviredScraperService
    {
        private readonly HttpClient _httpClient;
        private const string Url = "https://www.previred.com/indicadores-previsionales/";

        public PreviredScraperService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<List<IndicadorDiario>> ObtenerIndicadores()
        {
            try
            {
                var html = await _httpClient.GetStringAsync(Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var resultados = new List<IndicadorDiario>();
                var fecha = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                // === 1. UTM / UTA ===
                ExtraerUtmUta(doc, ref resultados, fecha);

                // === 2. Renta mínima ===
                ExtraerRentaMinima(doc, ref resultados, fecha);

                // === 3. Tasa SIS ===
                ExtraerTasaSis(doc, ref resultados, fecha);

                // === 4. Rentas y topes imponibles ===
                ExtraerTopesImponibles(doc, ref resultados, fecha);

                // === 5. Tasas AFP ===
                ExtraerTasasAfp(doc, ref resultados, fecha);

                // === 6. Seguro de Cesantía (AFC) ===
                ExtraerSeguroCesantia(doc, ref resultados, fecha);

                Console.WriteLine($"✅ Previred: {resultados.Count} indicadores extraídos.");
                return resultados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en Previred: {ex.Message}");
                return new List<IndicadorDiario>();
            }
        }

        // --- Métodos auxiliares ---
        private void ExtraerUtmUta(HtmlDocument doc, ref List<IndicadorDiario> resultados, DateTime fecha)
        {
            var tabla = doc.DocumentNode.SelectSingleNode("//td[strong[text()='UTM']]/ancestor::table");
            if (tabla?.SelectNodes(".//tr") is var filas && filas?.Count > 1)
            {
                var celdas = filas[1].SelectNodes("td");
                if (celdas?.Count >= 3)
                {
                    if (ParseMoneda(celdas[1].InnerText, out var utm))
                        resultados.Add(new IndicadorDiario { Codigo = "UTM", Fecha = fecha, Valor = utm, Fuente = "Previred" });
                    if (ParseMoneda(celdas[2].InnerText, out var uta))
                        resultados.Add(new IndicadorDiario { Codigo = "UTA", Fecha = fecha, Valor = uta, Fuente = "Previred" });
                }
            }
        }

        private void ExtraerRentaMinima(HtmlDocument doc, ref List<IndicadorDiario> resultados, DateTime fecha)
        {
            var node = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Trab. Dependientes e Independientes')]/following-sibling::td[1]");
            if (node != null && ParseMoneda(node.InnerText, out var valor))
                resultados.Add(new IndicadorDiario { Codigo = "RENTA_MINIMA", Fecha = fecha, Valor = valor, Fuente = "Previred" });
        }

        private void ExtraerTasaSis(HtmlDocument doc, ref List<IndicadorDiario> resultados, DateTime fecha)
        {
            var node = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Tasa SIS')]/following-sibling::td[1]//b");
            if (node == null) node = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Tasa SIS')]/following-sibling::td[1]");
            if (node != null && ParsePorcentaje(node.InnerText, out var tasa))
                resultados.Add(new IndicadorDiario { Codigo = "TASA_SIS", Fecha = fecha, Valor = tasa, Fuente = "Previred" });
        }

        private void ExtraerTopesImponibles(HtmlDocument doc, ref List<IndicadorDiario> resultados, DateTime fecha)
        {
            // AFP (87,8 UF)
            var afpNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Para afiliados a una AFP')]/following-sibling::td[1]");
            if (afpNode != null && ParseMoneda(afpNode.InnerText, out var topeAfp))
                resultados.Add(new IndicadorDiario { Codigo = "TOPE_AFP", Fecha = fecha, Valor = topeAfp, Fuente = "Previred" });

            // IPS (60 UF)
            var ipsNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Para afiliados al IPS')]/following-sibling::td[1]");
            if (ipsNode != null && ParseMoneda(ipsNode.InnerText, out var topeIps))
                resultados.Add(new IndicadorDiario { Codigo = "TOPE_IPS", Fecha = fecha, Valor = topeIps, Fuente = "Previred" });

            // Seguro de Cesantía (131,9 UF)
            var cesantiaNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Para Seguro de Cesantía')]/following-sibling::td[1]");
            if (cesantiaNode != null && ParseMoneda(cesantiaNode.InnerText, out var topeCesantia))
                resultados.Add(new IndicadorDiario { Codigo = "TOPE_CESANTIA", Fecha = fecha, Valor = topeCesantia, Fuente = "Previred" });
        }

        private void ExtraerTasasAfp(HtmlDocument doc, ref List<IndicadorDiario> resultados, DateTime fecha)
        {
            var filasAfp = doc.DocumentNode.SelectNodes("//td[contains(text(),'Capital') or contains(text(),'Cuprum') or contains(text(),'Habitat') or contains(text(),'PlanVital') or contains(text(),'ProVida') or contains(text(),'Modelo') or contains(text(),'Uno')]/parent::tr");
            if (filasAfp == null) return;

            foreach (var fila in filasAfp)
            {
                var celdas = fila.SelectNodes("td");
                if (celdas?.Count < 5) continue;

                var nombreAfp = celdas[0].InnerText.Trim();
                var codigoBase = $"AFP_{nombreAfp.ToUpper().Replace(" ", "_")}";

                if (ParsePorcentaje(celdas[1].InnerText, out var cargoTrab))
                    resultados.Add(new IndicadorDiario { Codigo = $"{codigoBase}_TRAB", Fecha = fecha, Valor = cargoTrab, Fuente = "Previred" });

                if (ParsePorcentaje(celdas[2].InnerText, out var cargoEmp))
                    resultados.Add(new IndicadorDiario { Codigo = $"{codigoBase}_EMP", Fecha = fecha, Valor = cargoEmp, Fuente = "Previred" });

                if (ParsePorcentaje(celdas[3].InnerText, out var totalPagar))
                    resultados.Add(new IndicadorDiario { Codigo = $"{codigoBase}_TOTAL", Fecha = fecha, Valor = totalPagar, Fuente = "Previred" });

                if (ParsePorcentaje(celdas[4].InnerText, out var independientes))
                    resultados.Add(new IndicadorDiario { Codigo = $"{codigoBase}_INDEP", Fecha = fecha, Valor = independientes, Fuente = "Previred" });
            }
        }

        private void ExtraerSeguroCesantia(HtmlDocument doc, ref List<IndicadorDiario> resultados, DateTime fecha)
        {
            var filas = doc.DocumentNode.SelectNodes("//td[contains(text(),'Plazo Indefinido') or contains(text(),'Plazo Fijo') or contains(text(),'11 años') or contains(text(),'Casa Particular')]/parent::tr");
            if (filas == null) return;

            foreach (var fila in filas)
            {
                var celdas = fila.SelectNodes("td");
                if (celdas == null || celdas.Count < 3) continue;

                var tipo = celdas[0].InnerText.Trim();
                string codigoEmp = null, codigoTrab = null;

                if (tipo.Contains("Plazo Indefinido") && !tipo.Contains("11"))
                {
                    codigoEmp = "CESANTIA_INDEF_EMP";
                    codigoTrab = "CESANTIA_INDEF_TRAB";
                }
                else if (tipo.Contains("Plazo Fijo"))
                {
                    codigoEmp = "CESANTIA_FIJO_EMP";
                }
                else if (tipo.Contains("11 años"))
                {
                    codigoEmp = "CESANTIA_11ANOS_EMP";
                }
                else if (tipo.Contains("Casa Particular"))
                {
                    codigoEmp = "CESANTIA_CASA_EMP";
                }

                if (codigoEmp != null && ParsePorcentaje(celdas[1].InnerText, out var valorEmp))
                    resultados.Add(new IndicadorDiario { Codigo = codigoEmp, Fecha = fecha, Valor = valorEmp, Fuente = "Previred" });

                if (codigoTrab != null && celdas[2].InnerText.Trim() != "–" && ParsePorcentaje(celdas[2].InnerText, out var valorTrab))
                    resultados.Add(new IndicadorDiario { Codigo = codigoTrab, Fecha = fecha, Valor = valorTrab, Fuente = "Previred" });
            }
        }

        // --- Parseo común ---
        private static bool ParseMoneda(string text, out decimal valor)
        {
            valor = 0;
            var limpio = text.Replace("$", "").Replace(".", "").Replace(",", ".").Trim();
            return decimal.TryParse(limpio, NumberStyles.Float, CultureInfo.InvariantCulture, out valor);
        }

        private static bool ParsePorcentaje(string text, out decimal valor)
        {
            valor = 0;
            var limpio = text.Replace("%", "").Replace(",", ".").Trim();
            // A veces viene con "R.I." → quitamos palabras
            limpio = Regex.Replace(limpio, @"[^0-9.,]", "");
            return decimal.TryParse(limpio, NumberStyles.Float, CultureInfo.InvariantCulture, out valor);
        }
    }
}