using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Models
{
    public class CourseDto
    {
        public Guid Id { get; set; }
        [MaxLength(150)]
        [Required]
        public string Title { get; set; }
        [MaxLength(1500)]
        [Required]
        public string Description { get; set; }

        public Guid AuthorId { get; set; }
    }
}
