using CourseLibrary.API.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.ValidationAttributes
{
    public class CourseTitleMustDifferFromDescripptionValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value,
            ValidationContext validationContext)
        {
            var courses = (IEnumerable<CourseForManipulationDto>)validationContext.ObjectInstance;
            foreach (var course in courses)
            {
                if (course.Title == course.Description)
                {
                    return new ValidationResult(ErrorMessage,
                        new[] { nameof(CourseForManipulationDto) });
                }

            }
                return ValidationResult.Success;
        }
    }
}
