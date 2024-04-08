using Smartway.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Smartway.Models
{
    public class Passport
    {
        private PassportType _type;
        private string? _number;

        public int Id { get; set; }

        [ForeignKey("Employee")]
        [JsonIgnore]
        public int EmployeeId { get; set; }
        public string? Type
        {
            get
            {
                return _type.ToString();
            }
            set
            {
                if (value != null)
                {
                    if (!Enum.IsDefined(typeof(PassportType), value)) throw new AppException("Паспорт может быть либо National, либо International");
                    _type = (PassportType)Enum.Parse(typeof(PassportType), value);
                } 
            }
        }

        [StringLength(10, ErrorMessage = "Номер паспорта должен быть не длиннее 10 символов")]
        public string? Number
        {
            get
            {
                return _number;
            }
            set
            {
                if (!Int64.TryParse(value, out long result)) throw new AppException("Номер паспорта должен быть числом");
                _number =  result.ToString();
            }
        }
    }

    public enum PassportType
    {
        National,
        International
    }
}
