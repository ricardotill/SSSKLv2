using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data
{
    public class OldUserMigration
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; }
    }
}
