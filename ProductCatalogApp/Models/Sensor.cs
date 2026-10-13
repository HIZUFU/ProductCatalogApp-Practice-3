using System.ComponentModel.DataAnnotations;

namespace ProductCatalogApp.Models
{
    public class Sensor : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название датчика")]
        [StringLength(100)]
        [Display(Name = "Название датчика")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите единицу измерения")]
        [StringLength(20)]
        [Display(Name = "Единица измерения")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "Нижняя граница нормы")]
        public double MinValue { get; set; }

        [Display(Name = "Верхняя граница нормы")]
        public double MaxValue { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Выберите оборудование")]
        [Display(Name = "Оборудование")]
        public int EquipmentId { get; set; }

        [Display(Name = "Оборудование")]
        public Equipment? Equipment { get; set; }

        public List<SensorReading> Readings { get; set; } = new();

        public bool IsOutOfRange(double value)
        {
            return value < MinValue || value > MaxValue;
        }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (MinValue >= MaxValue)
            {
                yield return new ValidationResult(
                    "Верхняя граница нормы должна быть больше нижней.",
                    new[] { nameof(MaxValue) });
            }
        }
    }
}
