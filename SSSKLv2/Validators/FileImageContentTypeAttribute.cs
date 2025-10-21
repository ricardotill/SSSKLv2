using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace SSSKLv2.Validators
{
    public class FileImageContentTypeAttribute : ValidationAttribute
    {
        private static readonly string[] AllowedImageTypes = new[]
        {
            "image/jpeg", "image/jpg", "image/png"
        };

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IBrowserFile file)
            {
                if (AllowedImageTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult(ErrorMessage ?? "Het geselecteerde bestand moet een afbeelding zijn.");
            }
            // If not required, let [Required] handle nulls
            return ValidationResult.Success;
        }
    }
}

