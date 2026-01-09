using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Kiosk.Models;

namespace Squash.Web.Areas.Kiosk.Controllers
{
    [Area("Kiosk")]
    public class RefereeController : Controller
    {
        private readonly IDataContext _dataContext;

        public RefereeController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public IActionResult Index()
        {
            var pin = HttpContext.Session.GetString("MatchPin");
            if (string.IsNullOrWhiteSpace(pin))
            {
                return Redirect("/m");
            }

            var match = _dataContext.Matches
                .Include(m => m.Draw)
                .Include(m => m.Court)
                .Include(m => m.Player1)!.ThenInclude(p => p.Nationality)
                .Include(m => m.Player2)!.ThenInclude(p => p.Nationality)
                .FirstOrDefault(m => m.PinCode == pin);

            if (match == null)
            {
                return Redirect("/m");
            }

            var model = new RefereeMatchViewModel
            {
                Draw = match.Draw?.Name ?? string.Empty,
                Court = match.Court?.Name ?? string.Empty,
                FirstPlayer = MapPlayer(match.Player1),
                SecondPlayer = MapPlayer(match.Player2),
                MatchGameId = match.Games.FirstOrDefault()?.Id ?? 0,
                GameScore = "0:0",
                CurrentGameScore = "0:0"
            };

            return View(model);
        }

        public IActionResult More()
        {
            return View();
        }

        private static RefereePlayerViewModel MapPlayer(Squash.DataAccess.Entities.Player? player)
        {
            if (player == null)
            {
                return new RefereePlayerViewModel();
            }

            var nationalityCode = player.Nationality?.Code ?? string.Empty;
            var nationalityName = player.Nationality?.Name ?? nationalityCode;

            return new RefereePlayerViewModel
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
