using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Models
{
    public class AuthorForCreationDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }
        [MaxLength(50)]
        [Required]
        public string LastName { get; set; }
        [Required]
        public DateTimeOffset DateOfBirth { get; set; }
        [MaxLength(50)]
        [Required]
        public string MainCategory { get; set; }
        public ICollection<CourseForCreationDto> Courses { get; set; }
          = new List<CourseForCreationDto>();
    }
}
