using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Helpers
{
    public static class DateTimeOffsetExtensions
    {
        public static int GetCurrentAge(this DateTimeOffset dateTimeOffset)
        {
            var currentDateTime = DateTime.UtcNow;
            int age = currentDateTime.Year - dateTimeOffset.Year;
            if (currentDateTime < dateTimeOffset.AddYears(1))
            {
                return age--;
            }
            return age;
        }
    }
}
