using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SSSKLv2.Data
{
    [Index(nameof(Username), IsUnique = true)]
    public class OldUserMigration : BaseModel
    {
        [Microsoft.Build.Framework.Required]
        [DisplayName("Gebruikersnaam")]
        public string Username { get; set; }
        [Microsoft.Build.Framework.Required]
        [RegularExpression(@"^\d+.\d{0,2}$",ErrorMessage = "Saldo moet 2 cijfers achter de komma hebben")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; }
    }
}
