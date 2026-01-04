using Microsoft.AspNetCore.Mvc;
using TimeWindow.api.App.Models;

namespace TimeWindow.api.Controllers
{
    public partial class GroupController : ControllerBase
    {
        private bool RequireDisplayName(string? displayName, out IActionResult error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = BadRequest(new { message = "displayName required" });
                return false;
            }
            return true;
        }

        private bool TryGetGroup(Guid groupId, out Group group, out IActionResult error)
        {
            group = _store.GetGroup(groupId);
            if (group == null)
            {
                error = NotFound(new { message = "group not found" });
                return false;
            }
            error = null;
            return true;
        }

        private bool EnsureMember(Guid groupId, string? displayName, out IActionResult error)
        {
            error = null;

            if (_store.IsMember(groupId, displayName) == false)
            {
                error = StatusCode(403, new { message = "not a member" });
                return false;
            }

            return true;
        }
    }
}
