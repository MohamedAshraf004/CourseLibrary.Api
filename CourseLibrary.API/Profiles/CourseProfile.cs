using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Profiles
{
    public class CourseProfile:Profile
    {
        public CourseProfile()
        {
            this.CreateMap<Course, CourseDto>().ReverseMap();
        }
    }
}
