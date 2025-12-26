using System;
using System.IO;
using System.Linq;
using System.Web;

namespace StudentStudyProgram.Infrastructure
{
    /// <summary>
    /// File upload validation helper
    /// </summary>
    public static class FileValidationHelper
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private static readonly string[] AllowedImageMimeTypes = { "image/jpeg", "image/png", "image/gif" };
        private const int MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB

        /// <summary>
        /// Validates uploaded image file for security
        /// </summary>
        public static ValidationResult ValidateImageFile(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Dosya seçilmedi." };
            }

            // Check file size
            if (file.ContentLength > MaxImageSizeBytes)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = $"Dosya boyutu çok büyük. Maksimum {MaxImageSizeBytes / (1024 * 1024)}MB olmalıdır." 
                };
            }

            // Check extension
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Geçersiz dosya türü. Sadece JPG, PNG ve GIF dosyaları kabul edilir." 
                };
            }

            // Check MIME type
            var mimeType = file.ContentType?.ToLowerInvariant();
            if (string.IsNullOrEmpty(mimeType) || !AllowedImageMimeTypes.Contains(mimeType))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Geçersiz dosya içeriği. Sadece resim dosyaları kabul edilir." 
                };
            }

            // Additional check: verify file signature (magic bytes) for common formats
            try
            {
                var buffer = new byte[8];
                file.InputStream.Position = 0;
                file.InputStream.Read(buffer, 0, 8);
                file.InputStream.Position = 0; // Reset stream position

                if (!IsValidImageSignature(buffer, extension))
                {
                    return new ValidationResult 
                    { 
                        IsValid = false, 
                        ErrorMessage = "Dosya içeriği uzantısıyla eşleşmiyor. Güvenlik nedeniyle reddedildi." 
                    };
                }
            }
            catch
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Dosya doğrulanamadı." 
                };
            }

            return new ValidationResult { IsValid = true };
        }

        private static bool IsValidImageSignature(byte[] buffer, string extension)
        {
            // Check magic bytes for common image formats
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    // JPEG: FF D8 FF
                    return buffer.Length >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;

                case ".png":
                    // PNG: 89 50 4E 47 0D 0A 1A 0A
                    return buffer.Length >= 8 &&
                           buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                           buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A;

                case ".gif":
                    // GIF: 47 49 46 38
                    return buffer.Length >= 4 &&
                           buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38;

                default:
                    return false;
            }
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}
