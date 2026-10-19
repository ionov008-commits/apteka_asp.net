using System.IO;

namespace WebApplication1.Services // Подставь свой namespace, если он другой
{
    public interface IQrCodeService
    {
        /// <summary>
        /// Генерирует QR-код из строки и возвращает его в формате Base64 для вставки в HTML (src атрибут)
        /// </summary>
        string GenerateQrCodeBase64(string text);
        byte[] GenerateQrCodeBytes(string text);

        /// <summary>
        /// Считывает QR-код из потока данных изображения и возвращает раскодированный текст
        /// </summary>
        string DecodeQrCode(Stream imageStream);
    }
}