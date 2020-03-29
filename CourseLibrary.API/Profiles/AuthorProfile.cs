using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Profiles
{
    public class AuthorProfile:Profile
    {
        public AuthorProfile()
        {
            this.CreateMap<Author, AuthorDto>()
                .ForMember(
                n => n.Name,
                op => op.MapFrom(scr => $"{scr.FirstName} {scr.LastName}")
                         )
                .ForMember(
                a => a.Age,
                op => op.MapFrom(scr => scr.DateOfBirth.GetCurrentAge()))
                .ReverseMap();
            this.CreateMap<AuthorForCreationDto, Author>().ReverseMap();
               
        }
    }
}
