using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QueueUnderflow.Data;
using QueueUnderflow.Models;

namespace QueueUnderflow.Controllers
{
    public class AnswersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AnswersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        [Authorize(Roles = "User, Admin")]
        [HttpPost]
        public IActionResult New(Answer answer)
        {
            var sanitizer = new HtmlSanitizer();
            answer.Date = DateTime.Now;

            if (ModelState.IsValid)
            {
                answer.Content = sanitizer.Sanitize(answer.Content);
                answer.Content = (answer.Content);
                db.Answers.Add(answer);
                db.SaveChanges();
                return RedirectToAction("Show", "Discussions", new { id = answer.DiscussionId });
            }
            else
            {
                return RedirectToAction("Show", "Discussions", new { id = answer.DiscussionId });
            }

        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            Answer answer = db.Answers.Find(id);

            if (answer.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Answers.Remove(answer);
                db.SaveChanges();
                return RedirectToAction("Show", "Discussions", new { id = answer.DiscussionId });
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-dash-circle-fill";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index", "Discussions");
            }
        }

        [Authorize(Roles = "User, Admin")]
        public IActionResult Edit(int id)
        {
            Answer answer = db.Answers.Find(id);

            if (answer.UserId == _userManager.GetUserId(User))
            {
                return View(answer);
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-dash-circle-fill";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index", "Discussions");
            }
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost]
        public IActionResult Edit(int id, Answer requestAnswer)
        {
            var sanitizer = new HtmlSanitizer();
            Answer answer = db.Answers.Find(id);
         
            if (answer.UserId == _userManager.GetUserId(User))
            {
                if (ModelState.IsValid)
                {
                    answer.Content = sanitizer.Sanitize(requestAnswer.Content);
                    answer.Content = (answer.Content);
                    db.SaveChanges();

                    return RedirectToAction("Show", "Discussions", new { id = answer.DiscussionId });
                }
                else
                {
                    return View(requestAnswer);
                }
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-dash-circle-fill";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index", "Discussions");

            }
        }
    }
}
