using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Controllers
{
    [Route("api/authors/{authorId}/Courses")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseLibraryRepository _courseLibraryRepository;
        private readonly IMapper _mapper;

        public CoursesController(ICourseLibraryRepository courseLibraryRepository,
            IMapper mapper)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        public ActionResult<IEnumerable<CourseDto>> GetCourses(Guid authorId)
        {
            if (!_courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var courses = _courseLibraryRepository.GetCourses(authorId);
            return Ok(_mapper.Map<IEnumerable<CourseDto>>(courses));
        }

        [HttpGet("{courseId}",Name = "GetCourse")]
        public ActionResult GetCourse(Guid authorId, Guid courseId)
        {
            if (!_courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var course = _courseLibraryRepository.GetCourse(authorId, courseId);
            if (course == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<CourseDto>(course));
        }

        [HttpPost]
        public ActionResult<CourseDto> CreateCourseForAuthor(
           Guid authorId, CourseForCreationDto course)
        {
            if (!_courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var courseEntity=_mapper.Map<Course>(course);
            _courseLibraryRepository.AddCourse(authorId, courseEntity);
            _courseLibraryRepository.Save();

            var courseToReturn = _mapper.Map<CourseDto>(courseEntity);
            return CreatedAtRoute("GetCourse",
                                new { authorId, courseId=courseToReturn.Id}
                                        ,courseToReturn);

        }
        [HttpOptions("~/options")]
        public IActionResult GetCoursessOptions()
        {
            Response.Headers.Add("Allow", "Get,Post,Option");
            return Ok();
        }
    }
}
