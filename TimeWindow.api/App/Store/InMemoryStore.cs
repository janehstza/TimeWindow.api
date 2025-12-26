using TimeWindow.api.App.Models;

namespace TimeWindow.api.App.Store
{
    public class InMemoryStore
    {
        private List<Group> _groups = new List<Group>();
        private List<Participant> _participants = new List<Participant>();
        private List<Preference> _preferences = new List<Preference>();
        private List<BusySlot> _busySlots = new List<BusySlot>();

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
        public bool HasSelectedTimeRange(Guid groupId, string displayName) { return false; }

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
    }
}