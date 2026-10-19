using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication1.Models;

[Table("pharmacists")] // Сопоставляем с твоей таблицей в БД
public class Pharmacist
{
    [Key]
    public int PharmacistId { get; set; }
    public int UserId { get; set; }
    public int PharmacyId { get; set; }
    public string FullName { get; set; }
    public string WorkingHours { get; set; }

    // Навигационные свойства тоже делаем virtual!
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    [ForeignKey("PharmacyId")]
    public virtual Pharmacy Pharmacy { get; set; }
}