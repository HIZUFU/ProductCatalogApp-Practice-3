using System.ComponentModel.DataAnnotations;

namespace ProductCatalogApp.Models
{
    public class SensorReading
    {
        public int Id { get; set; }

        [Display(Name = "Датчик")]
        public int SensorId { get; set; }

        public Sensor? Sensor { get; set; }

        [Display(Name = "Значение")]
        public double Value { get; set; }

        [Display(Name = "Время")]
        public DateTime Timestamp { get; set; }

        [StringLength(100)]
        [Display(Name = "Источник")]
        public string Source { get; set; } = string.Empty;

        [Display(Name = "Тревога")]
        public bool IsAlarm { get; set; }

        [Display(Name = "Квитировано")]
        public bool IsAcknowledged { get; set; }

        [StringLength(100)]
        [Display(Name = "Квитировал")]
        public string? AcknowledgedBy { get; set; }

        [Display(Name = "Время квитирования")]
        public DateTime? AcknowledgedAt { get; set; }
    }
}
