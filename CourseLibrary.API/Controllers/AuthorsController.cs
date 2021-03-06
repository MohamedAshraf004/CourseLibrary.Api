﻿using AutoMapper;
using CourseLibrary.API.Contracts.V1;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly ICourseLibraryRepository _courseLibraryRepository;
        private readonly IMapper _mapper;

        public AuthorsController(ICourseLibraryRepository courseLibraryRepository,
            IMapper mapper)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet()]
        public ActionResult<PagedResponse<AuthorDto>> GetAuthors(
                                [FromQuery] AuthorResourceParameters authorResourceParameters)
        {
            if (authorResourceParameters.MainCategory==null&&authorResourceParameters.SearchQuery ==null)
            {
                var authorsFromRepo = _courseLibraryRepository.GetAuthors();
                return Ok(new PagedResponse<AuthorDto>(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)));
            }
             var filteredAuthors = _courseLibraryRepository.GetAuthors(authorResourceParameters);
            return Ok(new PagedResponse<AuthorDto>(_mapper.Map<IEnumerable<AuthorDto>>(filteredAuthors)));

        }

        [HttpGet("{authorId}",Name = "GetAuthor")]
        public ActionResult<AuthorDto> GetAuthor(Guid authorId)
        {
            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }
             
            return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
        }

        [HttpPost]
        public ActionResult<AuthorDto> CreateAuthor(AuthorForCreationDto authorForCreationDto)
        {
            var author=_mapper.Map<Author>(authorForCreationDto);
            _courseLibraryRepository.AddAuthor(author);
            _courseLibraryRepository.Save();

            var authorToreturn = _mapper.Map<AuthorDto>(author);
            return CreatedAtRoute("GetAuthor", new { authorId = authorToreturn.Id }, authorToreturn);
        }

        [HttpDelete("{authorId}")]
        public IActionResult RemoveAuthor(Guid authorId)
        {
            var author = _courseLibraryRepository.GetAuthor(authorId);
            if (author == null)
            {
                return NotFound();
            }
            _courseLibraryRepository.DeleteAuthor(author);

            return NoContent();
        }

        [HttpOptions]
        public IActionResult GetAuthorsOptions()
        {
            Response.Headers.Add("Allow", "Get,Post,Option");
            return Ok();
        }
    }
}
