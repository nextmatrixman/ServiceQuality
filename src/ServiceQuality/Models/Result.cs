using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceQuality.Models
{
    public class Result
    {

        [Key]
        public int Id { get; set; }

        public virtual Service Service { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public int Order { get; set; }

    }
}
