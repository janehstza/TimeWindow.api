using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.AccessControl;
using TimeWindow.api.App.Models;

namespace TimeWindow.api.App.Store
{
    public class InMemoryStore
    {
        private List<Group> _groups = new();
        private List<Participant> _participants = new();
        private List<Preference> _preferences = new();
        private List<BusySlot> _busySlots = new();
        private List<AvailableSlot> _availableSlots = new();
        private readonly Dictionary<Guid, List<CommonTimeRangeDto>> _commonOptions = new();


        public Group CreateGroup(int targetCount)
        {
            var group = new Group();

            group.GroupId = Guid.NewGuid();
            group.InviteCode = GenerateInviteCode();
            group.TargetCount = targetCount;
            group.Status = "JOINING";
            group.CreatedAt = DateTime.Now;

            _groups.Add(group);

            return group;
        }
        private string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            return new string(
                Enumerable.Repeat(chars, 6)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray()
            );
        }

        public Participant JoinGroup(string inviteCode, string displayName)
        {
            var group = _groups.FirstOrDefault(g => g.InviteCode == inviteCode);

            var participant = new Participant
            {
                ParticipantId = Guid.NewGuid(),
                GroupId = group.GroupId,
                DisplayName = displayName,
                Submitted = false,
                JoinedAt = DateTime.Now
            };

            _participants.Add(participant);

            return participant;
        }

        public Group? GetGroup(Guid groupid)
        {
            return _groups.FirstOrDefault(g => g.GroupId == groupid);
        }
        public Group? GetGroupByInviteCode(string inviteCode)
        {
            return _groups.FirstOrDefault(g => g.InviteCode == inviteCode);
        }

        public int GetJoinedCount(Guid groupid)
        {
            return _participants.Count(p => p.GroupId == groupid); ;
        }

        public bool IsMember(Guid groupId, string displayName)
        {
            return _participants.Any(p => p.GroupId == groupId && p.DisplayName == displayName);
        }
        public int GetSubmittedCount(Guid groupId)
        {
            return _participants.Count(p => p.GroupId == groupId && p.Submitted == true);
        }

        public bool HasPreferences(Guid groupId, string displayName)
        {
            return _preferences.Any(r => r.GroupId == groupId && r.DisplayName == displayName);
        }
        public bool HasBusyCalendar(Guid groupId, string displayName)
        {
            return _busySlots.Any(s => s.GroupId == groupId && s.DisplayName == displayName);
        }

        public bool IsSubmitted(Guid groupId, string displayName)
        {
            return _participants.Any(p =>
                p.GroupId == groupId &&
                p.DisplayName == displayName &&
                p.Submitted == true
            );
        }

        public Preference UpsertPreference(Guid groupId, string displayName, int? budgetMin, int? budgetMax, int? hotelRating, bool transfer, string placesToGo)
        {
            var pref = _preferences.FirstOrDefault(f => f.GroupId == groupId && f.DisplayName == displayName);
            if (pref == null)
            {
                pref = new Preference
                {
                    GroupId = groupId,
                    DisplayName = displayName,
                };

                _preferences.Add(pref);
            }

            pref.BudgetMin = budgetMin;
            pref.BudgetMax = budgetMax;
            pref.HotelRating = hotelRating;
            pref.Transfer = transfer;
            pref.PlacesToGo = placesToGo;
            return pref;
        }
        public Preference? GetPreference(Guid groupId, string displayName)
        {
            var pref = _preferences.FirstOrDefault(f => f.GroupId == groupId && f.DisplayName == displayName);
            return pref;
        }

        public List<string> GetDistinctPlacesToGo(Guid groupId)
        {
            string raw = String.Join(",", _preferences.Where(d => d.GroupId == groupId).Select(p => p.PlacesToGo));
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length >0).Distinct().ToList();
        }
        public BusySlot AddBusySlot(Guid groupId, string displayName, DateTime startAt, DateTime endAt)
        {
            var busyslot = new BusySlot();
            busyslot.GroupId = groupId;
            busyslot.DisplayName = displayName;
            busyslot.StartAt = startAt;
            busyslot.EndAt = endAt;

            _busySlots.Add(busyslot);
            return busyslot;
        }

        public List<BusySlot> GetBusySlot(Guid groupId, string displayName)
        {

            return _busySlots.Where(s => s.GroupId == groupId
                                      && s.DisplayName == displayName)
                              .OrderBy(s => s.StartAt).ToList();

        }

        public void SetAvailableSlots(Guid groupId, string displayName, List<AvailableSlot> slots)
        {
            _availableSlots ??= new List<AvailableSlot>();

            _availableSlots.RemoveAll(s => s.GroupId == groupId && s.DisplayName == displayName);
            _availableSlots.AddRange(slots);
        }

        public List<AvailableSlot> GetAvailableSlots(Guid groupId, string displayName)
        {
            return _availableSlots.Where(s => s.GroupId == groupId
                                      && s.DisplayName == displayName)
                              .OrderBy(s => s.StartAt).ToList();
        }

        public IEnumerable<AvailableSlot> GetAvailableSlots(Guid groupId) => _availableSlots.Where(s => s.GroupId == groupId);


        public bool HasAvailableSlots(Guid groupId, string displayName)
        {
            return _availableSlots.Any(a => a.GroupId == groupId && a.DisplayName == displayName);
        }

        public void MarkSubmitted(Guid groupId, string displayName)
        {
            var p = _participants.FirstOrDefault(x => x.GroupId == groupId && x.DisplayName == displayName);
            if (p == null) return;
            p.Submitted = true;
        }

        public void SaveCommonOptions(Guid groupId, List<CommonTimeRangeDto> options)
        {
            _commonOptions[groupId] = options;
        }

        public List<CommonTimeRangeDto> GetCommonOptionsSnapshot(Guid groupId)
        {
           if( !_commonOptions.TryGetValue(groupId, out List<CommonTimeRangeDto> value)) 
            {
                return new List<CommonTimeRangeDto>();
            }
            
            return value;
        }
    }
}