using Microsoft.AspNetCore.Mvc;
using TimeWindow.api.App.Models;
using TimeWindow.api.App.Store;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TimeWindow.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class GroupController : ControllerBase
    {
        private static InMemoryStore _store = new InMemoryStore();

        [HttpPost]
        public IActionResult CreateGroup([FromBody] CreateGroupRequest request)
        {
            var group = _store.CreateGroup(request.TargetCount);
            group.TravelDays = request.TravelDays;
            group.DateStart = request.DateStart ?? DateOnly.FromDateTime(DateTime.Today);
            group.DateEnd = request.DateEnd ?? group.DateStart.AddDays(30);

            return Ok(group);
        }

        [HttpPost("join")]
        public IActionResult JoinGroup([FromBody] JoinGroupRequest request)
        {
            var participant = _store.JoinGroup(request.InviteCode, request.DisplayName);
            return Ok(participant);
        }

        [HttpGet("{groupId}")]
        public IActionResult GetGroupStatus(Guid groupId)
        {
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;

            var joinedCount = _store.GetJoinedCount(groupId);
            return Ok(new
            {
                groupId = group.GroupId,
                inviteCode = group.InviteCode,
                targetCount = group.TargetCount,
                joinedCount = joinedCount,
                status = group.Status
            });
        }

        [HttpGet("{groupId}/me/status")]
        public IActionResult GetMyStatus(Guid groupId, [FromQuery] string? displayName)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!EnsureMember(groupId, displayName, out var memberErr)) return memberErr;


            var joinedCount = _store.GetJoinedCount(groupId);
            var submittedCount = _store.GetSubmittedCount(groupId);

            var hasPreferences = _store.HasPreferences(groupId, displayName);
            var hasBusyCalendar = _store.HasBusyCalendar(groupId, displayName);
            var hasSelectedTimeRange = _store.HasAvailableSlots(groupId, displayName);
            var isSubmitted = _store.IsSubmitted(groupId, displayName);

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
        [HttpPut("{groupId}/preferences")]
        public IActionResult UpsertPreferences([FromRoute] Guid groupId, [FromQuery] string? displayName, [FromBody] UpsertPreferenceRequest request)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!EnsureMember(groupId, displayName, out var memberErr)) return memberErr;

            if (_store.IsSubmitted(groupId, displayName))
            {
                return Conflict(new { message = "submissions are locked" });
            }
            var pref = _store.UpsertPreference(groupId, displayName, request.BudgetMin, request.BudgetMax, request.HotelRating, request.Transfer, request.PlacesToGo);
            return Ok(pref);

        }

        [HttpGet("{groupId}/preferences")]
        public IActionResult GetPreference(Guid groupId, [FromQuery] string? displayName)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!EnsureMember(groupId, displayName, out var memberErr)) return memberErr;

            var pref = _store.GetPreference(groupId, displayName);
            if (pref == null)
            {
                return NotFound(new { message = "preference not found" });
            }
            return Ok(pref);
        }
        
        [HttpPost("{groupId}/busy")]
        public IActionResult AddBusySlot(Guid groupId, [FromQuery] string? displayName, [FromBody] BusySlotRequest req)
        {


            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!EnsureMember(groupId, displayName, out var memberErr)) return memberErr;

            if (req.EndAt <= req.StartAt) return BadRequest();

            
            var pref = _store.HasPreferences(groupId, displayName);
            if (!pref) return BadRequest(new { message = "preferences required" });


            var slot = _store.AddBusySlot(groupId, displayName, req.StartAt, req.EndAt);
            return Ok(slot);
        }

        [HttpGet("{groupId}/busy")]
        public IActionResult GetBusySlots([FromRoute] Guid groupId, [FromQuery] string? displayName)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!EnsureMember(groupId, displayName, out var memberErr)) return memberErr;

            return Ok(_store.GetBusySlot(groupId, displayName));
        }



        [HttpGet("{groupId}/available")]
        public IActionResult GetAvailableSlots([FromRoute] Guid groupId, [FromQuery] string? displayName)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!EnsureMember(groupId, displayName, out var memberErr)) return memberErr;

            return Ok(_store.GetAvailableSlots(groupId, displayName));
        }

        [HttpPost("{groupId}/available")]
        public IActionResult SubmitAvailableSlots([FromRoute] Guid groupId, [FromQuery] string? displayName, [FromBody] List<AvailableSlotInput> slots)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!EnsureMember(groupId, displayName, out var memberErr)) return memberErr;
            
            
            if (slots == null || slots.Count == 0) return BadRequest(new { message = "slots required" });

            if (slots.Any(s => s.EndAt <= s.StartAt))
                return BadRequest(new { message = "invalid time range" });
            if (_store.IsSubmitted(groupId, displayName))
                return Conflict(new { message = "already submitted" });


            var toSave = new List<AvailableSlot>();
            foreach (var s in slots)
            {
                toSave.Add(new AvailableSlot
                {
                    GroupId = groupId,
                    DisplayName = displayName,
                    StartAt = s.StartAt,
                    EndAt = s.EndAt
                });
            }

            _store.SetAvailableSlots(groupId, displayName, toSave);

            _store.MarkSubmitted(groupId, displayName);

            return Ok(toSave);
        }

        [HttpGet("{groupId}/common-options")]
        public IActionResult GetCommonTimeRanges([FromRoute] Guid groupId, [FromQuery] string? displayName, [FromQuery] int travelDays = 1)
        {
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            if (!RequireDisplayName(displayName, out var err)) return err;

            var submittedCount = _store.GetSubmittedCount(groupId);
            var targetCount = group.TargetCount;
            if (submittedCount != targetCount) return StatusCode(409, new { message = "not ready" });

            return Ok(ComputeCommonOptionsV2(groupId));
        }


        private List<CommonTimeRangeDto> ComputeCommonOptionsV2(Guid groupId)
        {
            var group = _store.GetGroup(groupId);
            if (group == null) return new List<CommonTimeRangeDto>();

            int n = group.TargetCount;
            int threshold = n / 2 + 1;
            var minDays = group.TravelDays;

            var slots = _store.GetAvailableSlots(groupId).ToList();
            if (slots.Count == 0) return new List<CommonTimeRangeDto>();

            var dayCount = new Dictionary<DateOnly, int>();
            foreach (var s in slots)
            {
                var sDate = DateOnly.FromDateTime(s.StartAt);
                var eDate = DateOnly.FromDateTime(s.EndAt);

                var slotStart = sDate < group.DateStart ? group.DateStart : sDate;
                var slotEnd = eDate > group.DateEnd ? group.DateEnd : eDate;

                if (slotStart > slotEnd) continue;

                for (var d = slotStart; d <= slotEnd; d = d.AddDays(1))
                {
                    dayCount[d] = dayCount.TryGetValue(d, out var c) ? c + 1 : 1;
                }
            }

            var goodDays = dayCount.Where(kv => kv.Value >= threshold)
                                   .Select(kv => kv.Key)
                                   .OrderBy(d => d)
                                   .ToList();

            var ranges = new List<CommonTimeRangeDto>();

            for (int i = 0; i < goodDays.Count; i++)
            {
                var startDay = goodDays[i];
                int currentIdx = i;
                int minAttendance = dayCount[startDay];

                while (currentIdx + 1 < goodDays.Count &&
                       goodDays[currentIdx + 1] == goodDays[currentIdx].AddDays(1))
                {
                    currentIdx++;

                    var countOnThisDay = dayCount[goodDays[currentIdx]];
                    if (countOnThisDay < minAttendance) minAttendance = countOnThisDay;
                }

                var endDay = goodDays[currentIdx];

                int duration = endDay.DayNumber - startDay.DayNumber + 1;

                if (duration >= minDays)
                {
                    ranges.Add(new CommonTimeRangeDto(startDay, endDay, duration, minAttendance));
                }

                i = currentIdx;
            }

            _store.SaveCommonOptions(groupId, ranges);
            return ranges;
        }
        [HttpGet("{groupId}/common-options/snapshot")]
        public IActionResult GetSnapshots([FromRoute] Guid groupId, [FromQuery] string? displayName)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;

            return Ok(_store.GetCommonOptionsSnapshot(groupId));
        }

        [HttpGet("{groupId}/options/snapshot")]
        public IActionResult GetOptionCardsSnapshot([FromRoute] Guid groupId, [FromQuery] string? displayName)
        {
            if (!RequireDisplayName(displayName, out var err)) return err;
            if (!TryGetGroup(groupId, out var group, out var groupErr)) return groupErr;
            
            var snap = _store.GetCommonOptionsSnapshot(groupId);
            if(snap.Count == 0 ) return StatusCode(409, new { message = "common options not ready" });

            var places = _store.GetDistinctPlacesToGo(groupId);
            if (places.Count == 0 ) return StatusCode(409, new { message = "places not ready" });

            var cards = new List<OptionCardDto>();
            foreach (var r in snap)
            {
                foreach (var p in places)
                {
                    cards.Add(new OptionCardDto
                    {
                        OptionId = $"{r.StartDate:yyyyMMdd}-{r.EndDate:yyyyMMdd}-{p}",
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                        Days = r.Days,
                        Location = p
                    });
                }
            }

            return Ok(cards);
        }
    }
}