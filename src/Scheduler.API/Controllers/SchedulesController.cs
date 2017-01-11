using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Scheduler.API.Core;
using Scheduler.API.ViewModels;
using Scheduler.Data.Abstract;
using Scheduler.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scheduler.API.Controllers
{
    [Route("api/[controller]")]
    public class SchedulesController : Controller
    {
        private IScheduleRepository _scheduleRepository;
        private IAttendeeRepository _attendeeRepository;
        private IUserRepository _userRepository;
        int page = 1;
        int pageSize = 4;

        public SchedulesController(IScheduleRepository scheduleRepository, IAttendeeRepository attendeeRepository, IUserRepository userRepository)
        {
            _scheduleRepository = scheduleRepository;
            _attendeeRepository = attendeeRepository;
            _userRepository = userRepository;
        }

        public IActionResult Get()
        {
            var pagination = Request.Headers["Pagination"];

            if (!string.IsNullOrEmpty(pagination))
            {
                string[] vals = pagination.ToString().Split(',');
                int.TryParse(vals[0], out page);
                int.TryParse(vals[1], out pageSize);
            }

            int currentPage = page;
            int currentPageSize = pageSize;
            int totalSchedules = _scheduleRepository.Count();
            var totalPages = (int)Math.Ceiling((double)totalSchedules / pageSize);

            IEnumerable<Schedule> _schedules = _scheduleRepository
                .AllIncluding(s => s.Creator, s => s.Attendees)
                .OrderBy(s => s.Id)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToList();

            Response.AddPagination(page, pageSize, totalSchedules, totalPages);

            IEnumerable<ScheduleViewModel> _schedulesVM = Mapper.Map<IEnumerable<Schedule>, IEnumerable<ScheduleViewModel>>(_schedules);

            return new OkObjectResult(_schedulesVM);
        }

        [HttpGet("{id}", Name = "GetSchedule")]
        public IActionResult Get(int id)
        {
            Schedule _schedule = _scheduleRepository
                .GetSingle(s => s.Id == id, s => s.Creator, s => s.Attendees);

            if (_schedule != null)
            {
                ScheduleViewModel _scheduleVM = Mapper.Map<Schedule, ScheduleViewModel>(_schedule);
                return new OkObjectResult(_scheduleVM);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/details", Name = "GetScheduleDetails")]
        public IActionResult GetScheduleDetails(int id)
        {
            Schedule _schedule = _scheduleRepository
                .GetSingle(s => s.Id == id, s => s.Creator, s => s.Attendees);

            if (_schedule != null)
            {
                ScheduleDetailsViewModel _scheduleDetailsVM = Mapper.Map<Schedule, ScheduleDetailsViewModel>(_schedule);

                foreach (var attendee in _schedule.Attendees)
                {
                    User _userDB = _userRepository.GetSingle(attendee.UserId);
                    _scheduleDetailsVM.Attendees.Add(Mapper.Map<User, UserViewModel>(_userDB));
                }

                return new OkObjectResult(_scheduleDetailsVM);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        public IActionResult Create([FromBody]ScheduleViewModel schedule)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Schedule _newSchedule = Mapper.Map<ScheduleViewModel, Schedule>(schedule);
            _newSchedule.DateCreated = DateTime.Now;

            _scheduleRepository.Add(_newSchedule);
            _scheduleRepository.Commit();

            foreach(var userId in schedule.Attendees)
            {
                _newSchedule.Attendees.Add(new Attendee { UserId = userId });
            }
            _scheduleRepository.Commit();

            schedule = Mapper.Map<Schedule, ScheduleViewModel>(_newSchedule);

            CreatedAtRouteResult result = CreatedAtRoute("GetSchedule", new { controller = "Schedules", id = schedule.Id }, schedule);
            return result;
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody]ScheduleViewModel schedule)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
        }
    }
}

        
