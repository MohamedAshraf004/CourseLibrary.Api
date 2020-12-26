using AutoMapper;
using CourseLibrary.API.Cache;
using CourseLibrary.API.Contracts.V1;
using CourseLibrary.API.Contracts.V1.Filters;
using CourseLibrary.API.Contracts.V1.Queries;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        private readonly IUriService _uriService;
        private readonly IMapper _mapper;

        public CoursesController(ICourseLibraryRepository courseLibraryRepository,
            IMapper mapper, IUriService uriService = null)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
            _uriService = uriService;
        }

        [HttpGet]
        [Cached(600)]
        public IActionResult GetCourses(Guid authorId, [FromQuery] PaginationQuery paginationQuery)
        {//check modelstate.isvalid not required as apicontroller take care of that
            if (!_courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var pagination = _mapper.Map<PaginationFilter>(paginationQuery);
            var courses = _courseLibraryRepository.GetCourses(authorId, pagination);
            if (pagination == null || pagination.PageNumber < 1 || pagination.PageSize < 1)
            {
                return Ok(new PagedResponse<CourseDto>(_mapper.Map<IEnumerable<CourseDto>>(courses)));
            }
            var paginationResponse = PaginationHelpers.CreatePaginatedResponse<Course>(_uriService, pagination, courses.ToList());
            return Ok(paginationResponse);
        }

        [HttpGet("{courseId}", Name = "GetCourse")]
        [Cached(600)]
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
            var courseEntity = _mapper.Map<Course>(course);
            _courseLibraryRepository.AddCourse(authorId, courseEntity);
            _courseLibraryRepository.Save();

            var courseToReturn = _mapper.Map<CourseDto>(courseEntity);
            return CreatedAtRoute("GetCourse",
                                new { authorId, courseId = courseToReturn.Id }
                                        , courseToReturn);

        }

        [HttpPut("{courseId}")]
        public IActionResult UpdateCourseForAuthor(Guid authorId,Guid courseId,CourseForUpdateDto courseForUpdate)
        {
            if (!_courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var course = _courseLibraryRepository.GetCourse(authorId, courseId);
            if (course==null)
            {
                var courseForEntity=_mapper.Map<Course>(courseForUpdate);
                courseForEntity.Id = courseId;
                _courseLibraryRepository.AddCourse(authorId, courseForEntity);
                _courseLibraryRepository.Save();

                var courseToReturn = _mapper.Map<CourseDto>(courseForEntity);

                return CreatedAtRoute("GetCourse",
                               new { authorId, courseId = courseToReturn.Id }
                                       , courseToReturn);

            }

            // apply the updated field values to that dto
            // map the CourseForUpdateDto back to an entity
            _mapper.Map(courseForUpdate, course);
            _courseLibraryRepository.UpdateCourse(course);
            _courseLibraryRepository.Save();

            return NoContent();
        }

        [HttpPatch("{courseId}")]
        public IActionResult PartiallyUpdateCourseForAuthor(Guid authorId,Guid courseId
                                        ,JsonPatchDocument<CourseForUpdateDto> patchDocument)
        {
            if (!_courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var courseFromRepo = _courseLibraryRepository.GetCourse(authorId, courseId);
            if (courseFromRepo==null)
            {
                var courseDto = new CourseForUpdateDto();
                patchDocument.ApplyTo(courseDto, ModelState);
                if (!TryValidateModel(courseDto))
                {
                    return ValidationProblem(ModelState);
                }

                var courseToAdd = _mapper.Map<Course>(courseDto);
                _courseLibraryRepository.AddCourse(authorId,courseToAdd);
                var courseToReturn = _mapper.Map<CourseDto>(courseToAdd);

                return CreatedAtRoute("GetCourse",
                               new { authorId, courseId = courseToReturn.Id }
                                       , courseToReturn);
            }

            var courseToPatch = _mapper.Map<CourseForUpdateDto>(courseFromRepo);
            patchDocument.ApplyTo(courseToPatch , ModelState);
            if (!TryValidateModel(courseToPatch))
            {
                return ValidationProblem(ModelState);
            }
            _mapper.Map(courseToPatch, courseFromRepo);
            _courseLibraryRepository.UpdateCourse(courseFromRepo);
            _courseLibraryRepository.Save();

            return NoContent();
        }

        [HttpDelete("{courseId}")]
        public IActionResult RemoveCourseForAuthor(Guid authorId, Guid courseId)
        {
            if (!_courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var course = _courseLibraryRepository.GetCourse(authorId, courseId);
            if (course==null)
            {
                return NotFound();
            }
            _courseLibraryRepository.DeleteCourse(course);
            _courseLibraryRepository.Save();

            return NoContent();
        }

        public override ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
        {
            var options=
            HttpContext.RequestServices.GetRequiredService<IOptions<ApiBehaviorOptions>>();

            return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
        }

        [HttpOptions("~/courses/options")]
        public IActionResult GetCoursessOptions()
        {
            Response.Headers.Add("Allow", "Get,Post,Option");
            return Ok();
        }
    }
}
