using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Build.Framework;

namespace SSSKLv2.Data
{
    public class OldUserMigration : BaseModel
    {
        [Required]
        [DisplayName("Gebruikersnaam")]
        public string Username { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; }
    }
}
