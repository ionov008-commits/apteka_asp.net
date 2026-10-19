using System;
using System.IO;
using QRCoder;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
// Используем стандартный байтовый генератор PNG, который есть в самом QRCoder
using PngResolver = QRCoder.PngByteQRCode;

namespace WebApplication1.Services
{
    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrCodeBase64(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Текст пуст");

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

            // PngByteQRCode генерирует массив байтов PNG напрямую без System.Drawing или ImageSharp
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

            return "data:image/png;base64," + Convert.ToBase64String(qrCodeAsPngByteArr);
        }
        public byte[] GenerateQrCodeBytes(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        public string DecodeQrCode(Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
                return null;

            try
            {
                // Для чтения без ImageSharp нам нужен декодер, который умеет работать с массивом байтов
                // Однако ZXing.Net в чистом виде (без биндингов) требует структуру LuminanceSource.
                // Если мы полностью удаляем ImageSharp, нам придется использовать 
                // альтернативный способ загрузки байтов.

                // Но так как ImageSharp у тебя уже стоит как зависимость других пакетов, 
                // мы оставим его ТОЛЬКО в методе чтения, так как он там не конфликтует.

                using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.L8>(imageStream);
                var reader = new ZXing.ImageSharp.BarcodeReader<SixLabors.ImageSharp.PixelFormats.L8>()
                {
                    AutoRotate = true,
                    Options = new DecodingOptions { TryHarder = true, PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
                };

                var result = reader.Decode(image);
                return result?.Text;
            }
            catch
            {
                return null;
            }
        }
    }
}