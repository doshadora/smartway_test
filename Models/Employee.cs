using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Smartway.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [ForeignKey("Company")]
        [JsonIgnore]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public List<Passport>? Passports { get; set; }

        [ForeignKey("Department")]
        [JsonIgnore]
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }
    }
}
