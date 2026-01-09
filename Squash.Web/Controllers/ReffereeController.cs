using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Models.Referee;

namespace Squash.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReffereeController : ControllerBase
    {
        private readonly IDataContext _dataContext;

        public ReffereeController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet("match")]
        public ActionResult<RefereeMatchResponse> GetMatch([FromQuery] string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
            {
                return Ok(new RefereeMatchResponse { Success = false });
            }

            var normalizedPin = pin.Trim();

            var match = _dataContext.Matches
                .Include(m => m.Draw)
                .Include(m => m.Court)
                .Include(m => m.Player1)!.ThenInclude(p => p.Nationality)
                .Include(m => m.Player2)!.ThenInclude(p => p.Nationality)
                .FirstOrDefault(m => m.PinCode == normalizedPin);

            if (match == null)
            {
                return Ok(new RefereeMatchResponse { Success = false });
            }

            var response = new RefereeMatchResponse
            {
                Success = true,
                Match = new RefereeMatch
                {
                    Draw = match.Draw?.Name ?? string.Empty,
                    Court = match.Court?.Name ?? string.Empty,
                    FirstPlayer = MapPlayer(match.Player1),
                    SecondPlayer = MapPlayer(match.Player2)
                }
            };

            return Ok(response);
        }

        private static RefereePlayer MapPlayer(Squash.DataAccess.Entities.Player? player)
        {
            if (player == null)
            {
                return new RefereePlayer();
            }

            var nationalityCode = player.Nationality?.Code ?? string.Empty;
            var nationalityName = player.Nationality?.Name ?? nationalityCode;

            return new RefereePlayer
            {
                Name = player.Name ?? string.Empty,
                PictureUrl = null,
                Nationality = nationalityName ?? string.Empty,
                NationalityFlagUrl = string.IsNullOrWhiteSpace(nationalityCode)
                    ? string.Empty
                    : $"/images/flags/{nationalityCode.ToLowerInvariant()}.png"
            };
        }
    }
}
