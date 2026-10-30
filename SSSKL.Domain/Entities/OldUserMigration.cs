using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKL.Domain.Entities
{
    public class OldUserMigration : BaseModel
    {
        [DisplayName("Gebruikersnaam")]
        public string Username { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; }
    }
}
