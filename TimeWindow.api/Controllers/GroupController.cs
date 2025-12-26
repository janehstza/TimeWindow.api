using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using TimeWindow.api.App.Models;
using TimeWindow.api.App.Store;

namespace TimeWindow.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private static InMemoryStore _store = new InMemoryStore();

        public class CreateGroupRequest
        {
            public int TargetCount { get; set; }
        }

        public class JoinGroupRequest
        {
            public string InviteCode { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }

        public class UpsertPreferenceRequest
        {
            public int? BudgetMin { get; set; }
            public int? BudgetMax { get; set; }
            public int? HotelRating { get; set; }
            public bool Transfer { get; set; }
            public string PlacesToGo { get; set; } = "";
        }

        [HttpPost]
        public IActionResult CreateGroup([FromBody] CreateGroupRequest request)
        {
            var group = _store.CreateGroup(request.TargetCount);
            return Ok(group);
        }

        [HttpPost("join")]
        public IActionResult JoinGroup([FromBody] JoinGroupRequest request)
        {
            var participant = _store.JoinGroup(request.InviteCode, request.DisplayName);
            return Ok(participant);
        }

        [HttpGet("{id}")]
        public IActionResult GetGroupStatus(Guid id)
        {
            var group = _store.GetGroup(id);
            if (group == null)
            {
                return NotFound();
            }

            var joinedCount = _store.GetJoinedCount(id);
            return Ok(new
            {
                groupId = group.GroupId,
                inviteCode = group.InviteCode,
                targetCount = group.TargetCount,
                joinedCount = joinedCount,
                status = group.Status
            });
        }

        [HttpGet("{id}/me/status")]
        public IActionResult GetMyStatus(Guid id, [FromQuery] string displayName)
        {
            var group = _store.GetGroup(id);
            if (group == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return BadRequest(new { message = "displayName is required" });
            }

            if (!_store.IsMember(id, displayName))
            {
                return StatusCode(403);
            }

            var joinedCount = _store.GetJoinedCount(id);
            var submittedCount = _store.GetSubmittedCount(id);

            var hasPreferences = _store.HasPreferences(id, displayName);
            var hasBusyCalendar = _store.HasBusyCalendar(id, displayName);
            var hasSelectedTimeRange = _store.HasSelectedTimeRange(id, displayName);
            var isSubmitted = _store.IsSubmitted(id, displayName);

            return Ok(new
            {
                groupId = group.GroupId,
                inviteCode = group.InviteCode,
                targetCount = group.TargetCount,
                status = group.Status,

                joinedCount,
                submittedCount,

                hasPreferences,
                hasBusyCalendar,
                hasSelectedTimeRange,
                isSubmitted
            });
        }
        [HttpPut("{id}/preferences")]
        public IActionResult UpsertPreferences(Guid id, [FromQuery] string displayName, [FromBody] UpsertPreferenceRequest request)
        {
            var group = _store.GetGroup(id);
            if (group == null)
            {
                return NotFound();
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return BadRequest(new { message = "displayName is required" });

            }
            if (!_store.IsMember(id, displayName)) return Forbid();

            if (_store.IsSubmitted(id, displayName))
            {
                return Conflict(new { message = "提交已鎖定，全員提交後將自動生成結果" });
            }
            var pref = _store.UpsertPreference(id, displayName, request.BudgetMin, request.BudgetMax, request.HotelRating, request.Transfer, request.PlacesToGo);
            return Ok(pref);

        }

        [HttpGet("{id}/preferences")]
        public IActionResult GetPreference(Guid id, [FromQuery] string displayName)
        {
            var group = _store.GetGroup(id);
            if (group == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return BadRequest(new { message = "displayName is required" });

            }
            if (!_store.IsMember(id, displayName)) return Forbid();
            var pref = _store.GetPreference(id, displayName);
            if (pref == null)
            {
                return NotFound();
            }
            return Ok(pref);
        }
        public class BusySlotRequest
        {
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
        }
        [HttpPost("{groupId}/busy")]
        public IActionResult AddBusySlot(Guid groupId, [FromQuery] string displayName, [FromBody] BusySlotRequest req)
        {


            if (string.IsNullOrWhiteSpace(displayName))
            {
                return BadRequest(new { message = "displayName is required" });

            }
            if (req.EndAt <= req.StartAt) return BadRequest();

            var group = _store.GetGroup(groupId);
            if (group == null) return NotFound();

            if (!_store.IsMember(groupId, displayName)) return StatusCode(403);

            var pref = _store.HasPreferences(groupId, displayName);
            if (!pref) return BadRequest(new { message = "preferences required" });


            var slot = _store.AddBusySlot(groupId, displayName, req.StartAt, req.EndAt);
            return Ok(slot);
        }

        [HttpGet("{groupId}/busy")]
        public IActionResult GetBusySlots([FromRoute] Guid groupId, [FromQuery] string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return BadRequest(new { message = "displayName is required" });
            var group = _store.GetGroup(groupId);
            if (group == null)
            {
                return NotFound();
            }

            if (!_store.IsMember(groupId, displayName)) return StatusCode(403);

            return Ok(_store.GetBusySlot(groupId, displayName));
        }



        [HttpGet("{groupId}/available")]
        public IActionResult GetAvailableSlots([FromRoute] Guid groupId, [FromQuery] string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return BadRequest(new { message = "displayName is required" });
            var group = _store.GetGroup(groupId);
            if (group == null)
            {
                return NotFound();
            }

            if (!_store.IsMember(groupId, displayName)) return StatusCode(403);

            return Ok(Array.Empty<string>());
        }

        [HttpPost("{groupId}/available")]
        public IActionResult SubmitAvailableSlots([FromRoute] Guid groupId, [FromQuery] string displayName, [FromBody] List<BusySlotRequest> slots)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return BadRequest(new { message = "displayName is required" });
            var group = _store.GetGroup(groupId);
            if (group == null)
            {
                return NotFound();
            }

            if (!_store.IsMember(groupId, displayName)) return StatusCode(403);

            if (slots == null || slots.Count == 0) return BadRequest(new { message = "slots required" });

            if (slots.Any(s => s.EndAt <= s.StartAt))
                return BadRequest(new { message = "無效的日期"});

            return Ok(new { count = slots.Count });

        }
    }
}