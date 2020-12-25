using CourseLibrary.API.Contracts.V1.Queries;
using System;

namespace CourseLibrary.API.Services
{
    public interface IUriService
    {
        Uri GetAllCoursesUri(PaginationQuery pagination = null);
        Uri GetPostUri(string postId);
    }
}