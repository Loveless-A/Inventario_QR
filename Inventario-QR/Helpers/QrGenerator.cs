using System.Text;
using QRCoder;

namespace Inventario_QR.Helpers
{
    /// <summary>
    /// Genera imagenes QR en el servidor para no depender de servicios externos
    /// (CDN) que dejarian los codigos rotos si el servidor no tiene internet.
    /// </summary>
    public static class QrGenerator
    {
        /// <summary>
        /// Devuelve el QR como un data URI de SVG, listo para un &lt;img src="..."&gt;.
        /// </summary>
        /// <remarks>
        /// Se usa <c>SvgQRCode</c> y no <c>PngByteQRCode</c> porque este ultimo
        /// depende de System.Drawing (Bitmap/Graphics), que lanza
        /// PlatformNotSupportedException en Linux y romperia la aplicacion
        /// dentro del contenedor. SVG es solo geometria, sin dependencias graficas.
        /// Ademas al ser vectorial las etiquetas de 3x3 cm imprimen mas nitidas
        /// que el PNG de 150x150 que usaba el CDN.
        /// </remarks>
        public static string ToSvgDataUri(string content, int pixelsPerModule = 6)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);

            var svg = new SvgQRCode(data).GetGraphic(pixelsPerModule);

            // El SVG solo contiene geometria: el texto codificado nunca aparece
            // en claro, asi que no hay riesgo de inyeccion con el Host de la
            // peticion. Se codifica en base64 para poder ponerlo en un <img src>
            // sin usar Html.Raw.
            return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        }
    }
}
