using Ganss.Xss;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QueueUnderflow.Data;
using QueueUnderflow.Models;
using System;
using System.Text.RegularExpressions;

namespace QueueUnderflow.Controllers
{
    public class DiscussionsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DiscussionsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // motorul de cautare
        [NonAction]
        public IQueryable<Discussion> Search(string searchValue)
        {

            if (searchValue == null || searchValue.Length == 0)
            {
                return null;
            }
            // regex care ia doar litere si cifre
            var regex = new Regex(@"\W+");
            // lista de keywords
            var keywords = regex.Split(searchValue.ToLower());
            // iau obiectele din baza de date
            var discussions = db.Discussions.Include("Answers").Include("Category");
            // max-heap: cele mai relevante discutii gasite
            var rez = new PriorityQueue<Discussion, int>(Comparer<int>.Create((a, b) => b - a));

            foreach (var discussion in discussions)
            {
                // fac split sa iau doar cuvintele sub forma de lista din titlu, description + raspunsuri
                var allWords = regex.Split(discussion.Title.ToLower()).Union(regex.Split(discussion.Content.ToLower())).ToList();
                foreach (var comm in discussion.Answers)
                {
                    allWords.Union(regex.Split(comm.Content.ToLower()));
                }

                // relevanta = cate keyword-uri se regasesc in titlu, descriere si raspunsuri
                int relevance = allWords.Intersect(keywords).Count();
                if (relevance > 0)
                {
                    rez.Enqueue(discussion, relevance);  // adaug in max-heap
                }
            }

            // scot in ordine obiectele din max-heap si le incarc intr-o lista
            List<Discussion> res = new List<Discussion>();
            while (rez.TryDequeue(out var obj, out var priority))
            {
                res.Add(obj);
            }

            // nu s-au gasit rezultate
            if (!res.Any())
            {
                return null;
            }

            return res.AsQueryable();
        }

        [HttpPost]
        public IActionResult Index(string searchValue)
        {
            var discussions = Search(searchValue);
            if (discussions == null)
            {
                ViewBag.IsNull = "null";
                return View();
            }
            else
            {
                ViewBag.Notification = $"Found {discussions.Count()} result(s)";
                ViewBag.Icon = "bi-search";
                ViewBag.Type = "bg-success";
            }
            
            ViewBag.Discussions = discussions;
            return View();
        }

        public IActionResult Index()
        {
            var discussions = db.Discussions.Include("Category");
            ViewBag.Discussions = discussions;

            if (TempData["notification"] != null)
            {
                ViewBag.Notification = TempData["notification"];
                ViewBag.Icon = TempData["icon"];
                ViewBag.Type = TempData["type"];
            }

            return View();
        }

        
        public IActionResult Show(int id)
        {
            Discussion discussion = db.Discussions
                .Include("Answers")
                .Include("User")
                .Include("Answers.User")
                .Where(disc => disc.Id == id).First();
            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            if (TempData["notification"] != null)
            {
                ViewBag.Notification = TempData["notification"];
                ViewBag.Icon = TempData["icon"];
                ViewBag.Type = TempData["type"];
            }

            return View( discussion );
        }

        [HttpPost]
        [Authorize(Roles = "User, Admin")]
        public IActionResult Show([FromForm] Answer answer)
        {
            var sanitizer = new HtmlSanitizer();
            answer.Date = DateTime.Now;
            answer.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                answer.Content = sanitizer.Sanitize(answer.Content);
                answer.Content = (answer.Content);
                db.Answers.Add(answer);
                db.SaveChanges();
                TempData["notification"] = "Comment added";
                TempData["icon"] = "bi-chat-text-fill";
                TempData["type"] = "bg-success";
                return RedirectToAction("Show", new { id = answer.DiscussionId });
            }
            else
            {
                Discussion disc = db.Discussions.Include("Category")
                                         .Include("User")
                                         .Include("Answers")
                                         .Include("Answers.User")
                                         .Where(disc => disc.Id == answer.DiscussionId)
                                         .First();

                return View(disc);
            }
        }

        [Authorize(Roles = "User, Admin")]
        public IActionResult New()
        {
            Discussion discussion = new Discussion();
            discussion.Categ = GetAllCategories();

            return View(discussion);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost]
        public IActionResult New(Discussion discussion)
        {
            var sanitizer = new HtmlSanitizer();
            discussion.Date = DateTime.Now;
            discussion.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                discussion.Content = sanitizer.Sanitize(discussion.Content);
                discussion.Content = (discussion.Content);
                db.Discussions.Add(discussion);
                db.SaveChanges();
                TempData["notification"] = "Discussion added";
                TempData["icon"] = "bi-plus-circle";
                TempData["type"] = "bg-success";
                return RedirectToAction("Index", "Categories");
            }
            else
            {
                discussion.Categ = GetAllCategories();
                return View(discussion);
            }
        }

        [Authorize(Roles = "User, Admin")]
        public IActionResult Edit(int id)
        {
            Discussion discussion = db.Discussions.Include("Category").Where(disc => disc.Id == id).First();
            discussion.Categ = GetAllCategories();

            if(discussion.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                ViewBag.CurrentUserId = _userManager.GetUserId(User);
                return View(discussion);
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-dash-circle-fill";
                TempData["type"] = "bg-danger";

                return RedirectToAction("Show", new { id = id});
            }
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost]
        public IActionResult Edit(int id, Discussion requestDiscussion)
        {
            var sanitizer = new HtmlSanitizer();
            Discussion discussion = db.Discussions.Find(id);
            if (User.IsInRole("Admin"))
            {
                if (ModelState.IsValid)
                {
                    discussion.CategoryId = requestDiscussion.CategoryId;

                    TempData["notification"] = "Discussion modified";
                    TempData["icon"] = "bi-pencil";
                    TempData["type"] = "bg-info";
                    db.SaveChanges();

                    return RedirectToAction("Show", new { id = id });
                }
                else
                {
                    requestDiscussion.Categ = GetAllCategories();
                    return View(requestDiscussion);
                }
            }
            else if (discussion.UserId == _userManager.GetUserId(User))
            {
                if (ModelState.IsValid)
                {
                    discussion.Content = sanitizer.Sanitize(requestDiscussion.Content);
                    discussion.Content = (discussion.Content);
                    discussion.Title = requestDiscussion.Title;
                    discussion.CategoryId = requestDiscussion.CategoryId;

                    TempData["notification"] = "Discussion modified";
                    TempData["icon"] = "bi-pencil";
                    TempData["type"] = "bg-info";
                    db.SaveChanges();

                    return RedirectToAction("Show", new { id = id });
                }
                else
                {
                    requestDiscussion.Categ = GetAllCategories();
                    return View(requestDiscussion);
                }
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-dash-circle-fill";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index", "Categories");
            }
        }


        [Authorize(Roles = "User, Admin")]
        [HttpPost]
        public IActionResult Delete(int id)
        {

            Discussion discussion = db.Discussions.Include("Answers").Where(disc => disc.Id == id).First();

            if (discussion.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Discussions.Remove(discussion);
                db.SaveChanges();

                TempData["notification"] = "Discussion deleted";
                TempData["icon"] = "bi-trash3";
                TempData["type"] = "bg-danger";
                db.SaveChanges();

                return RedirectToAction("Index", "Categories");
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-dash-circle-fill";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index", "Categories");
            }
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {

            var selectList = new List<SelectListItem>();


            var categories = from cat in db.Categories
                             select cat;

            foreach (var category in categories)
            {
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.CategoryName.ToString()
                });
            }

            return selectList;
        }
    }
}
