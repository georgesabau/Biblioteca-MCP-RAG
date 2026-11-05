using System.ComponentModel.DataAnnotations;
namespace Lab1_MVC.Models;

public class OrderEditViewModel
{
    public int OrderID { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Cititor")]
    public int CustomerID { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Carte")]
    public int BookID { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Data comenzii")]
    public DateTime? OrderDate { get; set; } = DateTime.Today;
}
