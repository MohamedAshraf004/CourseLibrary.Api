using AutoMapper;
using CourseLibrary.API.Contracts.V1.Filters;
using CourseLibrary.API.Contracts.V1.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseLibrary.API.Profiles
{
    public class DomainToFilterProfile:Profile
    {

        public DomainToFilterProfile()
        {
            this.CreateMap<PaginationQuery, PaginationFilter>();
        }
    }
}
