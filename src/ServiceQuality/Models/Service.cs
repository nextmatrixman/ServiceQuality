using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceQuality.Models
{
    public class Service
    {

        [Key]
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Type { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Url]
        [Display(Name = "URL")]
        public string Url { get; set; }

        [Required]
        [Range(0, 1000000)]
        public int Requests { get; set; }

        public bool Success { get; set; }

        public virtual ICollection<Result> Results { get; set; }

        public Service()
        {
            Results = new List<Result>();
        }

        public bool HasValidType()
        {
            return Type != null && (Type.Equals("Capacity") || Type.Equals("Distribution"));
        }
    }
}
