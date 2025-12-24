// Services/SiiScraperService.cs
using Indicadores.Models;
using System.Globalization;

namespace Indicadores.Services
{
    public class SiiScraperService
    {
        private readonly HttpClient _httpClient;

        public SiiScraperService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        }

        // === UF diaria ===
        public async Task<List<IndicadorDiario>> ObtenerUfDiaria(int anio)
        {
            var url = $"https://www.sii.cl/valores_y_fechas/uf/uf{anio}.htm";
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var tabla = doc.GetElementbyId("table_export");
            if (tabla == null) return new List<IndicadorDiario>();

            var resultados = new List<IndicadorDiario>();
            var filas = tabla.SelectNodes(".//tbody/tr");
            if (filas == null) return resultados;

            foreach (var fila in filas)
            {
                var diaNodo = fila.SelectSingleNode("th");
                if (diaNodo == null || !int.TryParse(diaNodo.InnerText.Trim(), out int dia)) continue;

                var celdas = fila.SelectNodes("td");
                if (celdas == null || celdas.Count != 12) continue;

                for (int mes = 0; mes < 12; mes++)
                {
                    var valorStr = celdas[mes]?.InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(valorStr) || valorStr == "&nbsp;") continue;

                    var valorLimpio = valorStr.Replace(".", "").Replace(",", ".");
                    if (!decimal.TryParse(valorLimpio, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor)) continue;

                    try
                    {
                        var fecha = new DateTime(anio, mes + 1, dia);
                        if (fecha <= DateTime.Today)
                            resultados.Add(new IndicadorDiario { Codigo = "UF", Fecha = fecha, Valor = valor, Fuente = "SII" });
                    }
                    catch { }
                }
            }
            return resultados;
        }

        // === UTM, UTA, IPC desde utm2025.htm ===
        public async Task<List<IndicadorDiario>> ObtenerUtmIpc(int anio)
        {
            var url = $"https://www.sii.cl/valores_y_fechas/utm/utm{anio}.htm";
            var html = await _httpClient.GetStringAsync(url);

            // Extraer tabla markdown de UTM/IPC
            var resultados = new List<IndicadorDiario>();
            var lineas = html.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var meses = new[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            foreach (var linea in lineas)
            {
                // Buscar líneas tipo: | Enero | 67.429 | 809.148 | 106,74 | ...
                if (!linea.Contains("|") || !meses.Any(m => linea.Contains(m))) continue;

                var celdas = linea.Split('|')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToArray();

                if (celdas.Length < 4) continue;

                var mesTexto = celdas[0];
                if (!meses.Contains(mesTexto)) continue;

                int mes = Array.IndexOf(meses, mesTexto) + 1;
                var fecha = new DateTime(anio, mes, 1);

                // UTM (celda 1)
                if (decimal.TryParse(celdas[1].Replace(".", "").Replace(",", "."), out var utm))
                    resultados.Add(new IndicadorDiario { Codigo = "UTM", Fecha = fecha, Valor = utm, Fuente = "SII" });

                // UTA (celda 2)
                if (decimal.TryParse(celdas[2].Replace(".", "").Replace(",", "."), out var uta))
                    resultados.Add(new IndicadorDiario { Codigo = "UTA", Fecha = fecha, Valor = uta, Fuente = "SII" });

                // IPC (celda 3)
                if (decimal.TryParse(celdas[3].Replace(".", "").Replace(",", "."), out var ipc))
                    resultados.Add(new IndicadorDiario { Codigo = "IPC", Fecha = fecha, Valor = ipc, Fuente = "SII" });
            }

            return resultados;
        }
    }
}