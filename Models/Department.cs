using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Smartway.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        [Phone]
        public string? Phone { get; set; }
    }
}
